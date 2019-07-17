﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fusee.Base.Common;
using Fusee.Base.Core;
using WebAssembly;
using FileMode = System.IO.FileMode;
using Path = Fusee.Base.Common.Path;

namespace Fusee.Base.Imp.WebAsm
{
    public static class WasmResourceLoader
    {
        public static string GetLocalAddress()
        {
            using (var window = (JSObject)Runtime.GetGlobalObject("window"))
            using (var location = (JSObject)window.GetObjectProperty("location"))
            {
                var address = (string)location.GetObjectProperty("href");

                if (address.Contains("/"))
                {
                    address = address.Substring(0, address.LastIndexOf('/') + 1);
                }

                return address;
            }
        }
    }

    /// <summary>
    /// Asset provider for direct file access. Typically used in desktop builds where assets are simply contained within
    /// a subdirectory of the application.
    /// </summary>
    public class AssetProvider : StreamAssetProvider
    {
        private string _baseDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAssetProvider"/> class.
        /// </summary>
        /// <param name="baseDir">The base directory where assets should be looked for.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public AssetProvider(string baseDir = null) : base()
        {
            _baseDir = (string.IsNullOrEmpty(baseDir)) ? "Assets" : baseDir;

            if (_baseDir[_baseDir.Length - 1] != '/')
                _baseDir += '/';
  
            // Image handler
            RegisterTypeHandler(new AssetHandler
            {
                ReturnedType = typeof(ImageData),
                Decoder = delegate (string id, object storage)
                {
                    string ext = Path.GetExtension(id).ToLower();
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                        case ".bmp":
                            return FileDecoder.LoadImage((Stream)storage);
                    }
                    return null;
                },
                Checker = delegate (string id)
                {
                    string ext = Path.GetExtension(id).ToLower();
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                        case ".bmp":
                            return true;
                    }
                    return false;
                }
            });

            // Text file -> String handler. Keep this one the last entry as it doesn't check the extension
            RegisterTypeHandler(new AssetHandler
            {
                ReturnedType = typeof(string),
                DecoderAsync = async (string id, object storage) =>
                {
                    var storageStream = (Stream)storage;
                    using (var streamReader = new StreamReader(storageStream, Encoding.ASCII))
                    {
                        return await streamReader.ReadToEndAsync();
                    }                 
                },
                Checker = id => true // If it's there, we can handle it...
            });
        }

        /// <summary>
        /// Creates a stream for the asset identified by id using <see cref="FileStream"/>
        /// </summary>
        /// <param name="id">The asset identifier.</param>
        /// <returns>
        /// A valid stream for reading if the asset ca be retrieved. null otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        [Obsolete("Use GetStreamAsync instead")]
        protected override Stream GetStream(string id)
        {
            var baseAddress = WasmResourceLoader.GetLocalAddress();
            var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
            var response = httpClient.GetAsync(id);
            return response.Result.Content.ReadAsStreamAsync().Result;
        }

        /// <summary>
        /// Creates an async stream for the asset identified by id using <see cref="FileStream"/>
        /// </summary>
        /// <param name="id">The asset identifier.</param>
        /// <returns>
        /// A valid stream for reading if the asset ca be retrieved. null otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        protected override async Task<Stream> GetStreamAsync(string id)
        {
            Console.WriteLine("Trying to get stream async");

            var baseAddress = WasmResourceLoader.GetLocalAddress();
            var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };

#if DEBUG
            Console.WriteLine($"Requesting '{id}' at '{baseAddress}'...");
#endif
            try
            {
                var response = await httpClient.GetAsync(id);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[Error] {nameof(WasmResourceLoader)}.{nameof(GetStreamAsync)}(): {exception}");
                return null;
            }
        }

        /// <summary>
        /// Checks the existence of the identified asset using <see cref="File.Exists"/>
        /// </summary>
        /// <param name="id">The asset identifier.</param>
        /// <returns>
        /// true if a stream can be created.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        [Obsolete("Use CheckExistsAsync() instead")]
        protected override bool CheckExists(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var baseAddress = WasmResourceLoader.GetLocalAddress();
            var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };

            var response = httpClient.GetAsync(id);
            return response.Result.StatusCode == System.Net.HttpStatusCode.OK;

      
            /*
            // If it is an absolute path (e.g. C:\SomeDir\AnAssetFile.ext) directly check its presence
            if (Path.IsPathRooted(id))
                return File.Exists(id);

            // Path seems relative. First see if the file exists at the current working directory
            if (File.Exists(id))
                return true;

            foreach (var baseDir in _baseDirs)
            {
                string path = Path.Combine(baseDir, id);
                if (File.Exists(path))
                    return true;
            }
            return false;
            */
        }

        protected override async Task<bool> CheckExistsAsync(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var baseAddress = WasmResourceLoader.GetLocalAddress();
            var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };

            var response = await httpClient.GetAsync(id);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
    }
}
