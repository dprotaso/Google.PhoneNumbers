/*
 * Copyright (C) 2014 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Google.PhoneNumbers
{
internal class MetadataManager {
  private const String ALTERNATE_FORMATS_FILE_PREFIX =
      @"Google.PhoneNumbers.Data.PhoneNumberAlternateFormatsProto";
  private const String SHORT_NUMBER_METADATA_FILE_PREFIX =
      @"Google.PhoneNumbers.Data.ShortNumberMetadataProto";

  private static readonly Logger LOGGER = Logger.getLogger(typeof(MetadataManager));

  private static readonly IDictionary<int, PhoneMetadata> callingCodeToAlternateFormatsMap =
      new SynchronizedDictionary<int, PhoneMetadata>();
  private static readonly IDictionary<String, PhoneMetadata> regionCodeToShortNumberMetadataMap =
      new SynchronizedDictionary<String, PhoneMetadata>();

  // A set of which country calling codes there are alternate format data for. If the set has an
  // entry for a code, then there should be data for that code linked into the resources.
  private static readonly ISet<int> countryCodeSet =
      AlternateFormatsCountryCodeSet.getCountryCodeSet();

  // A set of which region codes there are short number data for. If the set has an entry for a
  // code, then there should be data for that code linked into the resources.
  private static readonly ISet<String> regionCodeSet = ShortNumbersRegionCodeSet.getRegionCodeSet();

  internal MetadataManager() {
  }

  private static void loadAlternateFormatsMetadataFromFile(int countryCallingCode)
  {
    var resourceName = ALTERNATE_FORMATS_FILE_PREFIX + "_" + countryCallingCode;
    var assembly = typeof(MetadataManager).GetTypeInfo().Assembly;

      try
      {
          using (var stream = assembly.GetManifestResourceStream(resourceName))
          {
              using (var reader = new BinaryReader(stream))
              {
                  PhoneMetadataCollection alternateFormats = new PhoneMetadataCollection();
                  alternateFormats.readExternal(reader);
                  foreach (PhoneMetadata metadata in alternateFormats.getMetadataList())
                  {
                      callingCodeToAlternateFormatsMap.Add(metadata.getCountryCode(), metadata);
                  }
              }
          }
      }
      catch (IOException e)
      {
          LOGGER.log(Level.WARNING, e.ToString());
      }
  }

  internal static PhoneMetadata getAlternateFormatsForCountry(int countryCallingCode) {
    if (!countryCodeSet.Contains(countryCallingCode)) {
      return null;
    }
    lock (callingCodeToAlternateFormatsMap) {
      if (!callingCodeToAlternateFormatsMap.ContainsKey(countryCallingCode)) {
        loadAlternateFormatsMetadataFromFile(countryCallingCode);
      }
    }
    return callingCodeToAlternateFormatsMap[countryCallingCode];
  }

  private static void loadShortNumberMetadataFromFile(String regionCode) {
    var resourceName = SHORT_NUMBER_METADATA_FILE_PREFIX + "_" + regionCode;
    var assembly = typeof(MetadataManager).GetTypeInfo().Assembly;

    try
    {
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (var reader = new BinaryReader(stream))
            {
                PhoneMetadataCollection shortNumberMetadata = new PhoneMetadataCollection();
                shortNumberMetadata.readExternal(reader);
                foreach (PhoneMetadata metadata in shortNumberMetadata.getMetadataList())
                {
                    regionCodeToShortNumberMetadataMap.Add(regionCode, metadata);
                }
            }
        }
    }
    catch (IOException e)
    {
        LOGGER.log(Level.WARNING, e.ToString());
    }
  }

  // @VisibleForTesting
  internal static ISet<String> getShortNumberMetadataSupportedRegions() {
    return regionCodeSet;
  }

  internal static PhoneMetadata getShortNumberMetadataForRegion(String regionCode) {
    if (!regionCodeSet.Contains(regionCode)) {
      return null;
    }
    lock (regionCodeToShortNumberMetadataMap) {
      if (!regionCodeToShortNumberMetadataMap.ContainsKey(regionCode)) {
        loadShortNumberMetadataFromFile(regionCode);
      }
    }
    return regionCodeToShortNumberMetadataMap[regionCode];
  }
}
}
