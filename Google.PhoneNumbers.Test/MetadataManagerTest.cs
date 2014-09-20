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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Google.PhoneNumbers.Test
{
    [TestClass]
    [DeploymentItem(@"Data\", @"Data\")]
    public class MetadataManagerTest
    {
        [TestMethod]
        public void testAlternateFormatsContainsData()
        {
            // We should have some data for Germany.
            PhoneMetadata germanyAlternateFormats = MetadataManager.getAlternateFormatsForCountry(49);
            Assert.IsNotNull(germanyAlternateFormats);
            Assert.IsTrue(germanyAlternateFormats.numberFormats().Count > 0);
        }

        [TestMethod]
        public void testShortNumberMetadataContainsData()
        {
            // We should have some data for France.
            PhoneMetadata franceShortNumberMetadata = MetadataManager.getShortNumberMetadataForRegion("FR");
            Assert.IsNotNull(franceShortNumberMetadata);
            Assert.IsTrue(franceShortNumberMetadata.HasShortCode());
        }

        [TestMethod]
        public void testAlternateFormatsFailsGracefully()
        {
            PhoneMetadata noAlternateFormats = MetadataManager.getAlternateFormatsForCountry(999);
            Assert.IsNull(noAlternateFormats);
        }

        [TestMethod]
        public void testShortNumberMetadataFailsGracefully()
        {
            PhoneMetadata noShortNumberMetadata = MetadataManager.getShortNumberMetadataForRegion("XXX");
            Assert.IsNull(noShortNumberMetadata);
        }
    }
}
