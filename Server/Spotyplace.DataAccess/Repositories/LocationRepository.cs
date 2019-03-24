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

        public async Task<IEnumerable<Location>> GetOfUserAsync(Guid userId, bool includeFloors)
        {
            var query = _db.Locations
                .AsNoTracking()
                .Where(e => e.OwnerId == userId)
                .OrderByDescending(e => e.ModifiedAt)
                .AsQueryable();

            if (includeFloors)
            {
                query = query.Include(e => e.Floors);
            }

            return await query.ToListAsync();
        }

        public async Task<Location> GetLocationAsync(Guid id, bool includeFloors)
        {
            var query = _db.Locations
                .AsNoTracking()
                .Where(e => e.LocationId == id)
                .AsQueryable();

            if (includeFloors)
            {
                query = query.Include(e => e.Floors);
            }

            return await query.FirstOrDefaultAsync();
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

        public async Task<Floor> GetFloorAsync(Guid id, bool includeLocation)
        {
            var query = _db.Floors
                .AsNoTracking()
                .Where(e => e.FloorId == id)
                .AsQueryable();

            if (includeLocation)
            {
                query = query.Include(e => e.Location);
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}
