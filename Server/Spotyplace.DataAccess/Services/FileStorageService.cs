﻿using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Spotyplace.Entities.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Spotyplace.DataAccess.Services
{
    public class FileStorageService : IFileStorageService
    {
        private IAmazonS3 _s3Client { get; set; }
        private readonly UploadOptions _uploadOptions;

        public FileStorageService(IAmazonS3 s3Client, IOptionsMonitor<UploadOptions> uploadOptions)
        {
            _s3Client = s3Client;
            _uploadOptions = uploadOptions.CurrentValue;
        }

        public async Task<bool> UploadFileAsync(IFormFile file, string fileName)
        {
            try
            {
                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(file.OpenReadStream(), _uploadOptions.BucketName, string.Format("{0}{1}", _uploadOptions.BasePath, fileName));
            }
            catch(Exception)
            {
                return false;
            }
            return true;
        }

        public async Task<Stream> ReadFileAsync(string fileName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _uploadOptions.BucketName,
                    Key = string.Format("{0}{1}", _uploadOptions.BasePath, fileName)
                };

                using (var response = await _s3Client.GetObjectAsync(request))
                using (var responseStream = response.ResponseStream)
                {
                    var stream = new MemoryStream();
                    await responseStream.CopyToAsync(stream);
                    stream.Position = 0;
                    return stream;
                }
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
