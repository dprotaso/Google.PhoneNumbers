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
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Google.PhoneNumbers.Test
{
    [TestClass]
    public class ExampleNumbersTest
    {
        private static readonly Logger LOGGER = Logger.getLogger(typeof (ExampleNumbersTest));

        private PhoneNumberUtil phoneNumberUtil =
            PhoneNumberUtil.createInstance(PhoneNumberUtil.DEFAULT_METADATA_LOADER);

        private ShortNumberInfo shortNumberInfo;
        private List<PhoneNumber> invalidCases = new List<PhoneNumber>();
        private List<PhoneNumber> wrongTypeCases = new List<PhoneNumber>();

        public ExampleNumbersTest()
        {
            shortNumberInfo = new ShortNumberInfo(phoneNumberUtil);
        }

        /**
   * @param exampleNumberRequestedType  type we are requesting an example number for
   * @param possibleExpectedTypes       acceptable types that this number should match, such as
   *     FIXED_LINE and FIXED_LINE_OR_MOBILE for a fixed line example number.
   */

        private void checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType exampleNumberRequestedType,
            ISet<PhoneNumberUtil.PhoneNumberType> possibleExpectedTypes)
        {
            foreach (String regionCode in phoneNumberUtil.getSupportedRegions())
            {
                PhoneNumber exampleNumber =
                    phoneNumberUtil.getExampleNumberForType(regionCode, exampleNumberRequestedType);
                if (exampleNumber != null)
                {
                    if (!phoneNumberUtil.isValidNumber(exampleNumber))
                    {
                        invalidCases.Add(exampleNumber);
                        LOGGER.log(Level.SEVERE, "Failed validation for " + exampleNumber.ToString());
                    }
                    else
                    {
                        // We know the number is valid, now we check the type.
                        PhoneNumberUtil.PhoneNumberType exampleNumberType = phoneNumberUtil.getNumberType(exampleNumber);
                        if (!possibleExpectedTypes.Contains(exampleNumberType))
                        {
                            wrongTypeCases.Add(exampleNumber);
                            LOGGER.log(Level.SEVERE, "Wrong type for " +
                                                     exampleNumber.ToString() +
                                                     ": got " + exampleNumberType);
                            LOGGER.log(Level.WARNING, "Expected types: ");
                            foreach (PhoneNumberUtil.PhoneNumberType type in possibleExpectedTypes)
                            {
                                LOGGER.log(Level.WARNING, type.ToString());
                            }
                        }
                    }
                }
            }
        }

        [TestMethod] public void testFixedLine()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> fixedLineTypes = EnumSet.of(
                PhoneNumberUtil.PhoneNumberType.FIXED_LINE,
                PhoneNumberUtil.PhoneNumberType.FIXED_LINE_OR_MOBILE);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.FIXED_LINE, fixedLineTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testMobile()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> mobileTypes = EnumSet.of(PhoneNumberUtil.PhoneNumberType.MOBILE,
                PhoneNumberUtil.PhoneNumberType.FIXED_LINE_OR_MOBILE);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.MOBILE, mobileTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testTollFree()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> tollFreeTypes = EnumSet.of(PhoneNumberUtil.PhoneNumberType.TOLL_FREE);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.TOLL_FREE, tollFreeTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testPremiumRate()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> premiumRateTypes =
                EnumSet.of(PhoneNumberUtil.PhoneNumberType.PREMIUM_RATE);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.PREMIUM_RATE, premiumRateTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testVoip()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> voipTypes = EnumSet.of(PhoneNumberUtil.PhoneNumberType.VOIP);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.VOIP, voipTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testPager()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> pagerTypes = EnumSet.of(PhoneNumberUtil.PhoneNumberType.PAGER);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.PAGER, pagerTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testUan()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> uanTypes = EnumSet.of(PhoneNumberUtil.PhoneNumberType.UAN);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.UAN, uanTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testVoicemail()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> voicemailTypes = EnumSet.of(PhoneNumberUtil.PhoneNumberType.VOICEMAIL);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.VOICEMAIL, voicemailTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testSharedCost()
        {
            ISet<PhoneNumberUtil.PhoneNumberType> sharedCostTypes =
                EnumSet.of(PhoneNumberUtil.PhoneNumberType.SHARED_COST);
            checkNumbersValidAndCorrectType(PhoneNumberUtil.PhoneNumberType.SHARED_COST, sharedCostTypes);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testCanBeInternationallyDialled()
        {
            foreach (String regionCode in phoneNumberUtil.getSupportedRegions())
            {
                PhoneNumber exampleNumber = null;
                PhoneNumberDesc desc =
                    phoneNumberUtil.getMetadataForRegion(regionCode).getNoInternationalDialling();
                try
                {
                    if (desc.HasExampleNumber())
                    {
                        exampleNumber = phoneNumberUtil.parse(desc.getExampleNumber(), regionCode);
                    }
                }
                catch (NumberParseException e)
                {
                    LOGGER.log(Level.SEVERE, e.ToString());
                }
                if (exampleNumber != null && phoneNumberUtil.canBeInternationallyDialled(exampleNumber))
                {
                    wrongTypeCases.Add(exampleNumber);
                    LOGGER.log(Level.SEVERE, "Number " + exampleNumber.ToString()
                                             + " should not be internationally diallable");
                }
            }
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testGlobalNetworkNumbers()
        {
            foreach (var callingCode in phoneNumberUtil.getSupportedGlobalNetworkCallingCodes())
            {
                PhoneNumber exampleNumber =
                    phoneNumberUtil.getExampleNumberForNonGeoEntity(callingCode);
                Assert.IsNotNull(exampleNumber, "No example phone number for calling code " + callingCode);
                if (!phoneNumberUtil.isValidNumber(exampleNumber))
                {
                    invalidCases.Add(exampleNumber);
                    LOGGER.log(Level.SEVERE, "Failed validation for " + exampleNumber.ToString());
                }
            }
            Assert.AreEqual(0, invalidCases.Count);
        }

        [TestMethod] public void testEveryRegionHasAnExampleNumber()
        {
            foreach (String regionCode in phoneNumberUtil.getSupportedRegions())
            {
                PhoneNumber exampleNumber = phoneNumberUtil.getExampleNumber(regionCode);
                Assert.IsNotNull(exampleNumber, "None found for region " + regionCode);
            }
        }

        [TestMethod] public void testShortNumbersValidAndCorrectCost()
        {
            List<String> invalidStringCases = new List<String>();
            foreach (String regionCode in shortNumberInfo.getSupportedRegions())
            {
                String exampleShortNumber = shortNumberInfo.getExampleShortNumber(regionCode);
                if (!shortNumberInfo.isValidShortNumberForRegion(exampleShortNumber, regionCode))
                {
                    String invalidStringCase = "region_code: " + regionCode + ", national_number: " +
                                               exampleShortNumber;
                    invalidStringCases.Add(invalidStringCase);
                    LOGGER.log(Level.SEVERE, "Failed validation for string " + invalidStringCase);
                }
                PhoneNumber phoneNumber = phoneNumberUtil.parse(exampleShortNumber, regionCode);
                if (!shortNumberInfo.isValidShortNumber(phoneNumber))
                {
                    invalidCases.Add(phoneNumber);
                    LOGGER.log(Level.SEVERE, "Failed validation for " + phoneNumber.ToString());
                }

                foreach (
                    ShortNumberInfo.ShortNumberCost cost in Enum.GetValues(typeof (ShortNumberInfo.ShortNumberCost)))
                {
                    exampleShortNumber = shortNumberInfo.getExampleShortNumberForCost(regionCode, cost);
                    if (!exampleShortNumber.Equals(""))
                    {
                        if (cost != shortNumberInfo.getExpectedCostForRegion(exampleShortNumber, regionCode))
                        {
                            wrongTypeCases.Add(phoneNumber);
                            LOGGER.log(Level.SEVERE, "Wrong cost for " + phoneNumber.ToString());
                        }
                    }
                }
            }
            Assert.AreEqual(0, invalidStringCases.Count);
            Assert.AreEqual(0, invalidCases.Count);
            Assert.AreEqual(0, wrongTypeCases.Count);
        }

        [TestMethod] public void testEmergency()
        {
            int wrongTypeCounter = 0;
            foreach (String regionCode in shortNumberInfo.getSupportedRegions())
            {
                PhoneNumberDesc desc =
                    MetadataManager.getShortNumberMetadataForRegion(regionCode).getEmergency();
                if (desc.HasExampleNumber())
                {
                    String exampleNumber = desc.getExampleNumber();
                    if (!Regex.IsMatch(exampleNumber, desc.getPossibleNumberPattern()) ||
                        !shortNumberInfo.isEmergencyNumber(exampleNumber, regionCode))
                    {
                        wrongTypeCounter++;
                        LOGGER.log(Level.SEVERE, "Emergency example number test failed for " + regionCode);
                    }
                    else if (shortNumberInfo.getExpectedCostForRegion(exampleNumber, regionCode) !=
                             ShortNumberInfo.ShortNumberCost.TOLL_FREE)
                    {
                        wrongTypeCounter++;
                        LOGGER.log(Level.WARNING, "Emergency example number not toll free for " + regionCode);
                    }
                }
            }
            Assert.AreEqual(0, wrongTypeCounter);
        }

        [TestMethod] public void testCarrierSpecificShortNumbers()
        {
            int wrongTagCounter = 0;
            foreach (String regionCode in shortNumberInfo.getSupportedRegions())
            {
                // Test the carrier-specific tag.
                PhoneNumberDesc desc =
                    MetadataManager.getShortNumberMetadataForRegion(regionCode).getCarrierSpecific();
                if (desc.HasExampleNumber())
                {
                    String exampleNumber = desc.getExampleNumber();
                    PhoneNumber carrierSpecificNumber = phoneNumberUtil.parse(exampleNumber, regionCode);
                    if (!Regex.IsMatch(exampleNumber, desc.getPossibleNumberPattern()) ||
                        !shortNumberInfo.isCarrierSpecific(carrierSpecificNumber))
                    {
                        wrongTagCounter++;
                        LOGGER.log(Level.SEVERE, "Carrier-specific test failed for " + regionCode);
                    }
                }
                // TODO: Test other tags here.
            }
            Assert.AreEqual(0, wrongTagCounter);
        }
    }

    public static class EnumSet
    {
        public static HashSet<T> of<T>(params T[] entries)
        {
            return new HashSet<T>(entries);
        }
    }
}
