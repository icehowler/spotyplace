﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spotyplace.Business.Utils;
using Spotyplace.DataAccess.Repositories;
using Spotyplace.DataAccess.Services;
using Spotyplace.Entities.Config;
using Spotyplace.Entities.Core;
using Spotyplace.Entities.DTOs;
using Spotyplace.Entities.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spotyplace.Business.Managers
{
    public class FloorManager
    {
        private readonly ILogger _logger;
        private readonly ILocationRepository _locationRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly AccountManager _accountManager;
        private readonly UploadOptions _uploadOptions;
        private readonly PermissionManager _permissionManager;

        public FloorManager(ILocationRepository locationRepository, IFloorRepository floorRepository, IFileStorageService fileStorageService, AccountManager accountManager, IOptionsMonitor<UploadOptions> uploadOptions, ILogger<FloorManager> logger, PermissionManager permissionManager)
        {
            _locationRepository = locationRepository;
            _floorRepository = floorRepository;
            _fileStorageService = fileStorageService;
            _accountManager = accountManager;
            _uploadOptions = uploadOptions.CurrentValue;
            _logger = logger;
            _permissionManager = permissionManager;
        }

        /// <summary>
        /// Check if floor name is valid.
        /// </summary>
        /// <param name="name">Floor name to check.</param>
        /// <returns></returns>
        private bool IsFloorNameValid(string name)
        {
            return !RegexHelper.HasSpecialCharacters(name) && name.Length > 2;
        }

        /// <summary>
        /// Create a new floor to a location.
        /// </summary>
        /// <param name="floor">Floor model.</param>
        /// <param name="file">Image file.</param>
        /// <param name="locationId">Location id to map the floor to.</param>
        /// <param name="userEmail">Current user email.</param>
        /// <returns></returns>
        public async Task<bool> CreateFloorAsync(Guid locationId, FloorCreateRequestDto floor, IFormFile file, string userEmail)
        {
            floor.Name = floor.Name.Trim();

            // Check floor name
            if (!IsFloorNameValid(floor.Name))
            {
                return false;
            }

            // Get current user id
            var user = await _accountManager.GetAccountInfoAsync(userEmail);
            if (user == null)
            {
                return false;
            }

            // Get location to edit and check user rights
            var currentLocation = await _locationRepository.GetLocationAsync(locationId, true, false, false);
            if (!_permissionManager.CanEditLocation(user, currentLocation))
            {
                return false;
            }

            // Check file size
            if (file.Length == 0 || file.Length > _uploadOptions.MaxFileSize)
            {
                return false;
            }

            // Check file type
            var isSvg = false;
            ImageStreamInfo imageInfo;
            try
            {
                imageInfo = ImageHelper.ConvertImage(file, ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                imageInfo = null;
            }

            if (imageInfo == null)
            {
                // Check if svg
                imageInfo = ImageHelper.GetSvgInfo(file);
                isSvg = true;
            }

            var newFloor = new Floor(floor)
            {
                IsSvg = isSvg,
                MapWidth = imageInfo.Width,
                MapHeight = imageInfo.Height
            };
            currentLocation.Floors.Add(newFloor);
            await _locationRepository.EditAsync(currentLocation);

            // Upload map file
            if (isSvg)
            {
                await _fileStorageService.UploadFileAsync(file, ConcatHelper.GetFloorFileName(currentLocation.LocationId, newFloor.FloorId, isSvg));
            }
            else
            {
                await _fileStorageService.UploadFileAsync(imageInfo.Stream, ConcatHelper.GetFloorFileName(currentLocation.LocationId, newFloor.FloorId, isSvg));
            }

            return true;
        }

        /// <summary>
        /// Edit floor.
        /// </summary>
        /// <param name="id">Floor id to edit.</param>
        /// <param name="floor">Floor model.</param>
        /// <param name="file">Image file.</param>
        /// <param name="userEmail">Current user email.</param>
        /// <returns></returns>
        public async Task<bool> EditFloorAsync(Guid id, FloorCreateRequestDto floor, IFormFile file, string userEmail)
        {
            floor.Name = floor.Name.Trim();

            // Check floor name
            if (!IsFloorNameValid(floor.Name))
            {
                return false;
            }

            // Get current user id
            var user = await _accountManager.GetAccountInfoAsync(userEmail);
            if (user == null)
            {
                return false;
            }

            // Get location to edit and check user rights
            var currentFloor = await _floorRepository.GetFloorAsync(id, true, false, false);
            if (currentFloor == null || currentFloor.Location == null || !_permissionManager.CanEditLocation(user, currentFloor.Location))
            {
                return false;
            }

            if (file != null)
            {
                // Check file size
                if (file.Length > _uploadOptions.MaxFileSize)
                {
                    return false;
                }

                // Check file type
                var isSvg = false;
                ImageStreamInfo imageInfo;
                try
                {
                    imageInfo = ImageHelper.ConvertImage(file, ImageFormat.Jpeg);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    imageInfo = null;
                }

                if (imageInfo == null)
                {
                    // Check if svg
                    imageInfo = ImageHelper.GetSvgInfo(file);
                    isSvg = true;
                }

                await _fileStorageService.DeleteFileAsync(ConcatHelper.GetFloorFileName(currentFloor.LocationId, currentFloor.FloorId, currentFloor.IsSvg));

                currentFloor.MapWidth = imageInfo.Width;
                currentFloor.MapHeight = imageInfo.Height;
                currentFloor.IsSvg = isSvg;

                // Upload map file
                if (isSvg)
                {
                    await _fileStorageService.UploadFileAsync(file, ConcatHelper.GetFloorFileName(currentFloor.LocationId, currentFloor.FloorId, isSvg));
                }
                else
                {
                    await _fileStorageService.UploadFileAsync(imageInfo.Stream, ConcatHelper.GetFloorFileName(currentFloor.LocationId, currentFloor.FloorId, isSvg));
                }
            }

            currentFloor.Name = floor.Name;
            await _floorRepository.EditAsync(currentFloor);

            return true;
        }

        /// <summary>
        /// Delete floor.
        /// </summary>
        /// <param name="id">Floor id.</param>
        /// <param name="userEmail">Current user email.</param>
        /// <returns></returns>
        public async Task<bool> DeleteFloorAsync(Guid id, string userEmail)
        {
            // Get current user id
            var user = await _accountManager.GetAccountInfoAsync(userEmail);
            if (user == null)
            {
                return false;
            }

            // Get parent location and check user rights
            var floor = await _floorRepository.GetFloorAsync(id, true, false, false);
            if (floor == null || !_permissionManager.CanEditLocation(user, floor.Location))
            {
                return false;
            }

            await _fileStorageService.DeleteFileAsync(ConcatHelper.GetFloorFileName(floor.LocationId, floor.FloorId, floor.IsSvg));

            await _floorRepository.DeleteAsync(floor);
            return true;
        }

        /// <summary>
        /// Get floor image.
        /// </summary>
        /// <param name="id">Floor id.</param>
        /// <param name="userEmail">Current user email.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public async Task<(Stream, string)> GetFloorImage(Guid id, string userEmail, CancellationToken cancellationToken = default)
        {
            var floor = await _floorRepository.GetFloorAsync(id, true, false, false);

            // Return if no floor found
            if (floor == null)
            {
                return (null, null);
            }

            // Check if parent location is public
            if (!floor.Location.IsPublic)
            {
                // Check for authorization if private
                var user = await _accountManager.GetAccountInfoAsync(userEmail);
                var location = await _locationRepository.GetLocationAsync(floor.LocationId, false, true, false);
                if (!_permissionManager.CanViewLocation(user, location))
                {
                    return (null, null);
                }
            }

            var fileStream = await _fileStorageService.ReadFileAsync(ConcatHelper.GetFloorFileName(floor.LocationId, id, floor.IsSvg), cancellationToken);
            if (fileStream == null)
            {
                return (null, null);
            }

            return (fileStream, floor.IsSvg ? "image/svg+xml" : "image/png");
        }
    }
}
