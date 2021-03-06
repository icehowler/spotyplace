﻿using Microsoft.EntityFrameworkCore;
using Spotyplace.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotyplace.DataAccess.Repositories
{
    public class LocationRepository: ILocationRepository
    {
        private readonly CoreContext _db;

        public LocationRepository(CoreContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(Location location)
        {
            await _db.AddAsync(location);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Location>> GetOfUserAsync(Guid userId, bool includeFloors, bool includePublicUsers)
        {
            var query = _db.Locations
                .AsNoTracking()
                .Where(e => e.OwnerId == userId)
                .OrderBy(e => e.Name)
                .AsQueryable();

            if (includeFloors)
            {
                query = query.Include(e => e.Floors);
            }
            if (includePublicUsers)
            {
                query = query.Include(e => e.PublicUserLocations).ThenInclude(e => e.User);
            }

            return await query.ToListAsync();
        }

        public async Task<Location> GetLocationAsync(Guid id, bool includeFloors, bool includePublicUsers, bool tracking)
        {
            var query = _db.Locations
                .Where(e => e.LocationId == id)
                .AsQueryable();

            if (!tracking)
            {
                query = query.AsNoTracking();
            }
            if (includeFloors)
            {
                query = query.Include(e => e.Floors);
            }
            if (includePublicUsers)
            {
                query = query.Include(e => e.PublicUserLocations).ThenInclude(e => e.User);
            }

            var location = await query.FirstOrDefaultAsync();
            if (location != null && includeFloors)
            {
                location.Floors = location.Floors.OrderBy(f => f.Name).ToList();
            }

            return location;
        }

        public async Task EditAsync(Location location)
        {
            _db.Update(location);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Location location)
        {
            _db.Remove(location);
            await _db.SaveChangesAsync();
        }

        public async Task<ICollection<Location>> GetLocationsAsync(string keyword, Guid userId, string userDomain)
        {
            return await _db.Locations
                .AsNoTracking()
                .Where(e => e.IsSearchable &&
                    (e.IsPublic || e.OwnerId == userId || (e.IsPublicToSelected && (e.PublicSelectedGroup.Equals(userDomain) || e.PublicUserLocations.Any(u => u.UserId == userId)))) &&
                    EF.Functions.ILike(e.Name, string.Format("%{0}%", keyword)))
                .OrderBy(e => e.Name)
                .Take(10)
                .ToListAsync();
        }

        public async Task<ICollection<Location>> GetLatestLocationsAsync(Guid userId, string userDomain)
        {
            return await _db.Locations
                .AsNoTracking()
                .Include(e => e.PublicUserLocations)
                .Where(e => e.IsSearchable && (e.IsPublic || e.OwnerId == userId || (e.IsPublicToSelected && (e.PublicSelectedGroup.Equals(userDomain) || e.PublicUserLocations.Any(u => u.UserId == userId)))))
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();
        }
    }
}
