// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetManagedTemplatePackage : IManagedTemplatePackage
    {
        private const string AuthorKey = "Author";
        private const string LocalPackageKey = "LocalPackage";
        private const string OwnersKey = "Owners";
        private const string TrustedKey = "Trusted";
        private const string NuGetSourceKey = "NuGetSource";
        private const string PackageIdKey = "PackageId";
        private const string PackageVersionKey = "Version";
        private readonly IEngineEnvironmentSettings _settings;
        private readonly ILogger _logger;

        public NuGetManagedTemplatePackage(
          IEngineEnvironmentSettings settings,
          IInstaller installer,
          IManagedTemplatePackageProvider provider,
          string mountPointUri,
          string packageIdentifier)
        {
            if (string.IsNullOrWhiteSpace(mountPointUri))
            {
                throw new ArgumentException($"{nameof(mountPointUri)} cannot be null or empty", nameof(mountPointUri));
            }
            if (string.IsNullOrWhiteSpace(packageIdentifier))
            {
                throw new ArgumentException($"{nameof(packageIdentifier)} cannot be null or empty", nameof(packageIdentifier));
            }
            MountPointUri = mountPointUri;
            Installer = installer ?? throw new ArgumentNullException(nameof(installer));
            ManagedProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = settings.Host.LoggerFactory.CreateLogger<NuGetInstaller>();

            Details = new Dictionary<string, string>
            {
                [PackageIdKey] = packageIdentifier
            };
        }

        /// <summary>
        /// Private constructor used for de-serialization only.
        /// </summary>
        private NuGetManagedTemplatePackage(
            IEngineEnvironmentSettings settings,
            IInstaller installer,
            IManagedTemplatePackageProvider provider,
            string mountPointUri,
            IReadOnlyDictionary<string, string> details)
        {
            if (string.IsNullOrWhiteSpace(mountPointUri))
            {
                throw new ArgumentException($"{nameof(mountPointUri)} cannot be null or empty", nameof(mountPointUri));
            }
            MountPointUri = mountPointUri;
            Installer = installer ?? throw new ArgumentNullException(nameof(installer));
            ManagedProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Details = details?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? throw new ArgumentNullException(nameof(details));
            if (Details.TryGetValue(PackageIdKey, out string packageId))
            {
                if (string.IsNullOrWhiteSpace(packageId))
                {
                    throw new ArgumentException($"{nameof(details)} should contain key {PackageIdKey} with non-empty value", nameof(details));
                }
            }
            else
            {
                throw new ArgumentException($"{nameof(details)} should contain key {PackageIdKey}", nameof(details));
            }
            _logger = settings.Host.LoggerFactory.CreateLogger<NuGetInstaller>();
        }

        public string? Trusted
        {
            get => Details.TryGetValue(TrustedKey, out string trusted) ? trusted : false.ToString();

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Details.Remove(TrustedKey);
                }
                else
                {
                    Details[TrustedKey] = value!;
                }
            }
        }

        public string? Author
        {
            get => Details.TryGetValue(AuthorKey, out string author) ? author : null;

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Details[AuthorKey] = value!;
                }
                else
                {
                    _ = Details.Remove(AuthorKey);
                }
            }
        }

        public string? Owners
        {
            get => Details.TryGetValue(OwnersKey, out string owners) ? owners : null;

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Details[OwnersKey] = value!;
                }
                else
                {
                    _ = Details.Remove(OwnersKey);
                }
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Version) ? Identifier : $"{Identifier}::{Version}";

        public string Identifier => Details[PackageIdKey];

        public IInstaller Installer { get; }

        public DateTime LastChangeTime
        {
            get
            {
                try
                {
                    return _settings.Host.FileSystem.GetLastWriteTimeUtc(MountPointUri);
                }
                catch (Exception e)
                {
                    _logger.LogDebug($"Failed to get last changed time for {MountPointUri}, details: {e}");
                    return default;
                }
            }
        }

        public bool IsLocalPackage
        {
            get
            {
                if (Details.TryGetValue(LocalPackageKey, out string val) && bool.TryParse(val, out bool isLocalPackage))
                {
                    return isLocalPackage;
                }
                return false;
            }

            set
            {
                if (value)
                {
                    Details[LocalPackageKey] = true.ToString();
                }
                else
                {
                    _ = Details.Remove(LocalPackageKey);
                }
            }
        }

        public string MountPointUri { get; }

        public string? NuGetSource
        {
            get => Details.TryGetValue(NuGetSourceKey, out string nugetSource) ? nugetSource : null;

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Details[NuGetSourceKey] = value!;
                }
                else
                {
                    _ = Details.Remove(NuGetSourceKey);
                }
            }
        }

        public ITemplatePackageProvider Provider => ManagedProvider;

        public IManagedTemplatePackageProvider ManagedProvider { get; }

        public string? Version
        {
            get => Details.TryGetValue(PackageVersionKey, out string version) ? version : null;

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Details[PackageVersionKey] = value!;
                }
                else
                {
                    Details.Remove(PackageVersionKey);
                }
            }
        }

        internal Dictionary<string, string> Details { get; }

        public static NuGetManagedTemplatePackage Deserialize(
            IEngineEnvironmentSettings settings,
            IInstaller installer,
            IManagedTemplatePackageProvider provider,
            string mountPointUri,
            IReadOnlyDictionary<string, string> details)
        {
            return new NuGetManagedTemplatePackage(settings, installer, provider, mountPointUri, details);
        }

        public IReadOnlyDictionary<string, string> GetDetails()
        {
            Dictionary<string, string> details = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(Author))
            {
                details[AuthorKey] = Author!;
            }
            if (!string.IsNullOrWhiteSpace(Owners))
            {
                details[OwnersKey] = Owners!;
            }
            if (!string.IsNullOrWhiteSpace(Trusted))
            {
                details[TrustedKey] = Trusted!;
            }
            if (!string.IsNullOrWhiteSpace(NuGetSource))
            {
                details[NuGetSourceKey] = NuGetSource!;
            }
            return details;
        }
    }
}
