﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spotyplace.Business.Managers;
using Spotyplace.Entities.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Spotyplace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FloorController : Controller
    {
        private readonly LocationManager _locationManager;
        private readonly FloorManager _floorManager;

        public FloorController(LocationManager locationManager, FloorManager floorManager)
        {
            _locationManager = locationManager;
            _floorManager = floorManager;
        }

        [Authorize]
        [Route("{id:guid}")]
        [HttpPost]
        public async Task<IActionResult> CreateFloorAsync(Guid id)
        {
            if (Request.Form.Files.Count != 1)
            {
                return BadRequest();
            }

            var file = Request.Form.Files[0];
            var floor = new FloorCreateRequestDto(await Request.ReadFormAsync());
            var success = await _floorManager.CreateFloorAsync(id, floor, file, User.FindFirstValue(ClaimTypes.Email));

            if (success)
            {
                return Ok(true);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize]
        [Route("{id:guid}")]
        [HttpPut]
        public async Task<IActionResult> EditFloorAsync(Guid id)
        {
            var files = Request.Form.Files;
            var floor = new FloorCreateRequestDto(await Request.ReadFormAsync());
            var success = await _floorManager.EditFloorAsync(id, floor, files.Count == 1 ? files[0] : null, User.FindFirstValue(ClaimTypes.Email));

            if (success)
            {
                return Ok(true);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize]
        [Route("{id:guid}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteFloorAsync(Guid id)
        {
            var success = await _floorManager.DeleteFloorAsync(id, User.FindFirstValue(ClaimTypes.Email));

            if (success)
            {
                return Ok(true);
            }
            else
            {
                return BadRequest();
            }
        }

        [Route("{id:guid}/image")]
        public async Task<IActionResult> GetFloorImageAsync(Guid id, CancellationToken cancellationToken)
        {
            var (response, contentType) = await _floorManager.GetFloorImage(id, User.FindFirstValue(ClaimTypes.Email), cancellationToken);
            if (response == null)
            {
                return NotFound();
            }

            return new FileStreamResult(response, new MediaTypeHeaderValue(contentType).MediaType);
        }
    }
}
