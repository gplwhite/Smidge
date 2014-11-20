﻿using Microsoft.AspNet.Http;
using System;

namespace Fuze
{

    public static class HttpExtensions
    {
        public static void AddCompressionResponseHeader(this HttpContext context, CompressionType cType)
        {
            if (cType == CompressionType.deflate)
            {
                context.Response.Headers["Content-encoding"] = "deflate";
            }
            else if (cType == CompressionType.gzip)
            {
                context.Response.Headers["Content-encoding"] = "gzip";
            }
        }

        /// <summary>
        /// Check what kind of compression to use. Need to select the first available compression 
        /// from the header value as this is how .Net performs caching by compression so we need to follow
        /// this process.
        /// If IE 6 is detected, we will ignore compression as it's known that some versions of IE 6
        /// have issues with it.
        /// </summary>
        public static CompressionType GetClientCompression(this HttpContext context)
        {
            CompressionType type = CompressionType.none;
            var agentHeader = context.Request.Headers["User-Agent"];
            if (agentHeader != null && agentHeader.Contains("MSIE 6"))
            {
                return type;
            }

            string acceptEncoding = context.Request.Headers["Accept-Encoding"];

            if (!string.IsNullOrEmpty(acceptEncoding))
            {
                string[] supported = acceptEncoding.Split(',');
                //get the first type that we support
                for (var i = 0; i < supported.Length; i++)
                {
                    if (supported[i].Contains("deflate"))
                    {
                        type = CompressionType.deflate;
                        break;
                    }
                    else if (supported[i].Contains("gzip")) //sometimes it could be x-gzip!
                    {
                        type = CompressionType.gzip;
                        break;
                    }
                }
            }

            return type;
        }
    }
    
}