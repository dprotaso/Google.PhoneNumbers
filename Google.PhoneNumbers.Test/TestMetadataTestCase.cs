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
using System.IO;
using System.Reflection;

namespace Google.PhoneNumbers.Test
{
    public class TestMetadataTestCase
    {
        private static readonly String TEST_META_DATA_FILE_PREFIX =
            "Google.PhoneNumbers.Test.Data.PhoneNumberMetadataProtoForTesting";

        protected readonly PhoneNumberUtil phoneUtil;

        public TestMetadataTestCase()
        {
            phoneUtil = initializePhoneUtilForTesting();
        }

        private static PhoneNumberUtil initializePhoneUtilForTesting()
        {
            PhoneNumberUtil phoneUtil = new PhoneNumberUtil(
                TEST_META_DATA_FILE_PREFIX, new TestMetadataLoader(),
                CountryCodeToRegionCodeMapForTesting.getCountryCodeToRegionCodeMap());
            PhoneNumberUtil.setInstance(phoneUtil);
            return phoneUtil;
        }


        private class TestMetadataLoader : MetadataLoader
        {
            public Stream loadMetadata(string metadataFileName)
            {
                // Load the test data first
                var assembly = typeof(TestMetadataTestCase).GetTypeInfo().Assembly;
                var data = assembly.GetManifestResourceStream(metadataFileName);
                
                if (data == null)
                    data = PhoneNumberUtil.DEFAULT_METADATA_LOADER.loadMetadata(metadataFileName);

                return data;
            }
        }
    }
}
