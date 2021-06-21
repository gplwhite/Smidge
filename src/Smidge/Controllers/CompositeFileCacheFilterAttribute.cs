﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smidge.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Smidge.Cache;
using Microsoft.Extensions.FileProviders;
using Smidge.Options;

namespace Smidge.Controllers
{
    //TODO: Should this execute when debug = true?

    /// <summary>
    /// This checks the file system for an already persisted minified, combined, compressed file for the 
    /// request definition. If there is one it returns that file directly and the controller does not execute.
    /// </summary>
    public sealed class CompositeFileCacheFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new CacheFilter(
                serviceProvider.GetRequiredService<ISmidgeFileSystem>(),
                serviceProvider.GetRequiredService<CacheBusterResolver>(),
                serviceProvider.GetRequiredService<IBundleManager>());
        }

        /// <summary>
        /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
        /// can be reused across requests.
        /// </summary>
        public bool IsReusable => true;

        internal static bool TryGetCachedCompositeFileResult(ISmidgeFileSystem fileSystem, string cacheBusterValue, string filesetKey, CompressionType type, string mime,
            out FileResult result, out DateTime lastWriteTime)
        {
            result = null;
            lastWriteTime = DateTime.Now;

            var cacheFile = fileSystem.CacheFileSystem.GetCachedCompositeFile(cacheBusterValue, type, filesetKey, out _);
            if (cacheFile.Exists)
            {
                lastWriteTime = cacheFile.LastModified.DateTime;

                if (!string.IsNullOrWhiteSpace(cacheFile.PhysicalPath))
                {
                    //if physical path is available then it's the physical file system, in which case we'll deliver the file with the PhysicalFileResult
                    //FilePathResult uses IHttpSendFileFeature which is a native host option for sending static files
                    result = new PhysicalFileResult(cacheFile.PhysicalPath, mime);
                    return true;
                }
                else
                {
                    //deliver the file via stream
                    result = new FileStreamResult(cacheFile.CreateReadStream(), mime);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The internal filter that performs the lookup
        /// </summary>
        private class CacheFilter : IActionFilter
        {
            private readonly ISmidgeFileSystem _fileSystem;
            private readonly CacheBusterResolver _cacheBusterResolver;
            private readonly IBundleManager _bundleManager;

            public CacheFilter(ISmidgeFileSystem fileSystem, CacheBusterResolver cacheBusterResolver, IBundleManager bundleManager)
            {
                _fileSystem = fileSystem;
                _cacheBusterResolver = cacheBusterResolver;
                _bundleManager = bundleManager;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (!context.ActionArguments.Any()) return;
                var firstArg = context.ActionArguments.First().Value;
                RequestModel file = firstArg as RequestModel;

                if (file != null)
                {
                    var cacheBusterValue = file.ParsedPath.CacheBusterValue;

                    if (TryGetCachedCompositeFileResult(_fileSystem, cacheBusterValue, file.FileKey, file.Compression, file.Mime, out FileResult result, out DateTime lastWrite))
                    {
                        file.LastFileWriteTime = lastWrite;
                        context.Result = result;
                    }
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
            }
        }

    }
}