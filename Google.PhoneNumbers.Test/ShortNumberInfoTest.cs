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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Google.PhoneNumbers.Test
{
    [TestClass]
    public class ShortNumberInfoTest : TestMetadataTestCase
    {
        private ShortNumberInfo shortInfo;

        public ShortNumberInfoTest()
        {
            shortInfo = new ShortNumberInfo(phoneUtil);
        }

        [TestMethod] public void testIsPossibleShortNumber()
        {
            PhoneNumber possibleNumber = new PhoneNumber();
            possibleNumber.setCountryCode(33).setNationalNumber(123456L);
            Assert.IsTrue(shortInfo.isPossibleShortNumber(possibleNumber));
            Assert.IsTrue(shortInfo.isPossibleShortNumberForRegion("123456", RegionCode.FR));

            PhoneNumber impossibleNumber = new PhoneNumber();
            impossibleNumber.setCountryCode(33).setNationalNumber(9L);
            Assert.IsFalse(shortInfo.isPossibleShortNumber(impossibleNumber));
            Assert.IsFalse(shortInfo.isPossibleShortNumberForRegion("9", RegionCode.FR));

            // Note that GB and GG share the country calling code 44, and that this number is possible but
            // not valid.
            Assert.IsTrue(shortInfo.isPossibleShortNumber(
                new PhoneNumber().setCountryCode(44).setNationalNumber(11001L)));
        }

        [TestMethod] public void testIsValidShortNumber()
        {
            Assert.IsTrue(shortInfo.isValidShortNumber(
                new PhoneNumber().setCountryCode(33).setNationalNumber(1010L)));
            Assert.IsTrue(shortInfo.isValidShortNumberForRegion("1010", RegionCode.FR));
            Assert.IsFalse(shortInfo.isValidShortNumber(
                new PhoneNumber().setCountryCode(33).setNationalNumber(123456L)));
            Assert.IsFalse(shortInfo.isValidShortNumberForRegion("123456", RegionCode.FR));

            // Note that GB and GG share the country calling code 44.
            Assert.IsTrue(shortInfo.isValidShortNumber(
                new PhoneNumber().setCountryCode(44).setNationalNumber(18001L)));
        }

        [TestMethod] public void testGetExpectedCost()
        {
            String premiumRateExample = shortInfo.getExampleShortNumberForCost(
                RegionCode.FR, ShortNumberInfo.ShortNumberCost.PREMIUM_RATE);
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                shortInfo.getExpectedCostForRegion(premiumRateExample, RegionCode.FR));
            PhoneNumber premiumRateNumber = new PhoneNumber();
            premiumRateNumber.setCountryCode(33).setNationalNumber(int.Parse(premiumRateExample));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                shortInfo.getExpectedCost(premiumRateNumber));

            String standardRateExample = shortInfo.getExampleShortNumberForCost(
                RegionCode.FR, ShortNumberInfo.ShortNumberCost.STANDARD_RATE);
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.STANDARD_RATE,
                shortInfo.getExpectedCostForRegion(standardRateExample, RegionCode.FR));
            PhoneNumber standardRateNumber = new PhoneNumber();
            standardRateNumber.setCountryCode(33).setNationalNumber(int.Parse(standardRateExample));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.STANDARD_RATE,
                shortInfo.getExpectedCost(standardRateNumber));

            String tollFreeExample = shortInfo.getExampleShortNumberForCost(
                RegionCode.FR, ShortNumberInfo.ShortNumberCost.TOLL_FREE);
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCostForRegion(tollFreeExample, RegionCode.FR));
            PhoneNumber tollFreeNumber = new PhoneNumber();
            tollFreeNumber.setCountryCode(33).setNationalNumber(int.Parse(tollFreeExample));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCost(tollFreeNumber));

            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion("12345", RegionCode.FR));
            PhoneNumber unknownCostNumber = new PhoneNumber();
            unknownCostNumber.setCountryCode(33).setNationalNumber(12345L);
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCost(unknownCostNumber));

            // Test that an invalid number may nevertheless have a cost other than UNKNOWN_COST.
            Assert.IsFalse(shortInfo.isValidShortNumberForRegion("116123", RegionCode.FR));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCostForRegion("116123", RegionCode.FR));
            PhoneNumber invalidNumber = new PhoneNumber();
            invalidNumber.setCountryCode(33).setNationalNumber(116123L);
            Assert.IsFalse(shortInfo.isValidShortNumber(invalidNumber));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCost(invalidNumber));

            // Test a nonexistent country code.
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion("911", RegionCode.ZZ));
            unknownCostNumber.clear();
            unknownCostNumber.setCountryCode(123).setNationalNumber(911L);
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCost(unknownCostNumber));
        }

        [TestMethod] public void testGetExpectedCostForSharedCountryCallingCode()
        {
            // Test some numbers which have different costs in countries sharing the same country calling
            // code. In Australia, 1234 is premium-rate, 1194 is standard-rate, and 733 is toll-free. These
            // are not known to be valid numbers in the Christmas Islands.
            String ambiguousPremiumRateString = "1234";
            PhoneNumber ambiguousPremiumRateNumber = new PhoneNumber().setCountryCode(61)
                .setNationalNumber(1234L);
            String ambiguousStandardRateString = "1194";
            PhoneNumber ambiguousStandardRateNumber = new PhoneNumber().setCountryCode(61)
                .setNationalNumber(1194L);
            String ambiguousTollFreeString = "733";
            PhoneNumber ambiguousTollFreeNumber = new PhoneNumber().setCountryCode(61)
                .setNationalNumber(733L);

            Assert.IsTrue(shortInfo.isValidShortNumber(ambiguousPremiumRateNumber));
            Assert.IsTrue(shortInfo.isValidShortNumber(ambiguousStandardRateNumber));
            Assert.IsTrue(shortInfo.isValidShortNumber(ambiguousTollFreeNumber));

            Assert.IsTrue(shortInfo.isValidShortNumberForRegion(ambiguousPremiumRateString, RegionCode.AU));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                shortInfo.getExpectedCostForRegion(ambiguousPremiumRateString, RegionCode.AU));
            Assert.IsFalse(shortInfo.isValidShortNumberForRegion(ambiguousPremiumRateString, RegionCode.CX));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion(ambiguousPremiumRateString, RegionCode.CX));
            // PREMIUM_RATE takes precedence over UNKNOWN_COST.
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                shortInfo.getExpectedCost(ambiguousPremiumRateNumber));

            Assert.IsTrue(shortInfo.isValidShortNumberForRegion(ambiguousStandardRateString, RegionCode.AU));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.STANDARD_RATE,
                shortInfo.getExpectedCostForRegion(ambiguousStandardRateString, RegionCode.AU));
            Assert.IsFalse(shortInfo.isValidShortNumberForRegion(ambiguousStandardRateString, RegionCode.CX));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion(ambiguousStandardRateString, RegionCode.CX));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCost(ambiguousStandardRateNumber));

            Assert.IsTrue(shortInfo.isValidShortNumberForRegion(ambiguousTollFreeString, RegionCode.AU));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCostForRegion(ambiguousTollFreeString, RegionCode.AU));
            Assert.IsFalse(shortInfo.isValidShortNumberForRegion(ambiguousTollFreeString, RegionCode.CX));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion(ambiguousTollFreeString, RegionCode.CX));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCost(ambiguousTollFreeNumber));
        }

        [TestMethod] public void testGetExampleShortNumber()
        {
            Assert.AreEqual("8711", shortInfo.getExampleShortNumber(RegionCode.AM));
            Assert.AreEqual("1010", shortInfo.getExampleShortNumber(RegionCode.FR));
            Assert.AreEqual("", shortInfo.getExampleShortNumber(RegionCode.UN001));
            Assert.AreEqual("", shortInfo.getExampleShortNumber(null));
        }

        [TestMethod] public void testGetExampleShortNumberForCost()
        {
            Assert.AreEqual("3010", shortInfo.getExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.TOLL_FREE));
            Assert.AreEqual("1023", shortInfo.getExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.STANDARD_RATE));
            Assert.AreEqual("42000", shortInfo.getExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.PREMIUM_RATE));
            Assert.AreEqual("", shortInfo.getExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.UNKNOWN_COST));
        }

        [TestMethod] public void testConnectsToEmergencyNumber_US()
        {
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("911", RegionCode.US));
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("112", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("999", RegionCode.US));
        }

        [TestMethod] public void testConnectsToEmergencyNumberLongNumber_US()
        {
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("9116666666", RegionCode.US));
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("1126666666", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("9996666666", RegionCode.US));
        }

        [TestMethod] public void testConnectsToEmergencyNumberWithFormatting_US()
        {
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("9-1-1", RegionCode.US));
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("1-1-2", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("9-9-9", RegionCode.US));
        }

        [TestMethod] public void testConnectsToEmergencyNumberWithPlusSign_US()
        {
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("+911", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber(" +911", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("+112", RegionCode.US));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("+999", RegionCode.US));
        }

        [TestMethod] public void testConnectsToEmergencyNumber_BR()
        {
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("911", RegionCode.BR));
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("190", RegionCode.BR));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("999", RegionCode.BR));
        }

        [TestMethod] public void testConnectsToEmergencyNumberLongNumber_BR()
        {
            // Brazilian emergency numbers don't work when additional digits are appended.
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("9111", RegionCode.BR));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("1900", RegionCode.BR));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("9996", RegionCode.BR));
        }

        [TestMethod] public void testConnectsToEmergencyNumber_CL()
        {
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("131", RegionCode.CL));
            Assert.IsTrue(shortInfo.connectsToEmergencyNumber("133", RegionCode.CL));
        }

        [TestMethod] public void testConnectsToEmergencyNumberLongNumber_CL()
        {
            // Chilean emergency numbers don't work when additional digits are appended.
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("1313", RegionCode.CL));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("1330", RegionCode.CL));
        }

        [TestMethod] public void testConnectsToEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("911", RegionCode.AO));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("222123456", RegionCode.AO));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("923123456", RegionCode.AO));
        }

        [TestMethod] public void testConnectsToEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("911", RegionCode.ZW));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("01312345", RegionCode.ZW));
            Assert.IsFalse(shortInfo.connectsToEmergencyNumber("0711234567", RegionCode.ZW));
        }

        [TestMethod] public void testIsEmergencyNumber_US()
        {
            Assert.IsTrue(shortInfo.isEmergencyNumber("911", RegionCode.US));
            Assert.IsTrue(shortInfo.isEmergencyNumber("112", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("999", RegionCode.US));
        }

        [TestMethod] public void testIsEmergencyNumberLongNumber_US()
        {
            Assert.IsFalse(shortInfo.isEmergencyNumber("9116666666", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("1126666666", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("9996666666", RegionCode.US));
        }

        [TestMethod] public void testIsEmergencyNumberWithFormatting_US()
        {
            Assert.IsTrue(shortInfo.isEmergencyNumber("9-1-1", RegionCode.US));
            Assert.IsTrue(shortInfo.isEmergencyNumber("*911", RegionCode.US));
            Assert.IsTrue(shortInfo.isEmergencyNumber("1-1-2", RegionCode.US));
            Assert.IsTrue(shortInfo.isEmergencyNumber("*112", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("9-9-9", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("*999", RegionCode.US));
        }

        [TestMethod] public void testIsEmergencyNumberWithPlusSign_US()
        {
            Assert.IsFalse(shortInfo.isEmergencyNumber("+911", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber(" +911", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("+112", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("+999", RegionCode.US));
        }

        [TestMethod] public void testIsEmergencyNumber_BR()
        {
            Assert.IsTrue(shortInfo.isEmergencyNumber("911", RegionCode.BR));
            Assert.IsTrue(shortInfo.isEmergencyNumber("190", RegionCode.BR));
            Assert.IsFalse(shortInfo.isEmergencyNumber("999", RegionCode.BR));
        }

        [TestMethod] public void testIsEmergencyNumberLongNumber_BR()
        {
            Assert.IsFalse(shortInfo.isEmergencyNumber("9111", RegionCode.BR));
            Assert.IsFalse(shortInfo.isEmergencyNumber("1900", RegionCode.BR));
            Assert.IsFalse(shortInfo.isEmergencyNumber("9996", RegionCode.BR));
        }

        [TestMethod] public void testIsEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.IsFalse(shortInfo.isEmergencyNumber("911", RegionCode.AO));
            Assert.IsFalse(shortInfo.isEmergencyNumber("222123456", RegionCode.AO));
            Assert.IsFalse(shortInfo.isEmergencyNumber("923123456", RegionCode.AO));
        }

        [TestMethod] public void testIsEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.IsFalse(shortInfo.isEmergencyNumber("911", RegionCode.ZW));
            Assert.IsFalse(shortInfo.isEmergencyNumber("01312345", RegionCode.ZW));
            Assert.IsFalse(shortInfo.isEmergencyNumber("0711234567", RegionCode.ZW));
        }

        [TestMethod] public void testEmergencyNumberForSharedCountryCallingCode()
        {
            // Test the emergency number 112, which is valid in both Australia and the Christmas Islands.
            Assert.IsTrue(shortInfo.isEmergencyNumber("112", RegionCode.AU));
            Assert.IsTrue(shortInfo.isValidShortNumberForRegion("112", RegionCode.AU));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCostForRegion("112", RegionCode.AU));
            Assert.IsTrue(shortInfo.isEmergencyNumber("112", RegionCode.CX));
            Assert.IsTrue(shortInfo.isValidShortNumberForRegion("112", RegionCode.CX));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCostForRegion("112", RegionCode.CX));
            PhoneNumber sharedEmergencyNumber =
                new PhoneNumber().setCountryCode(61).setNationalNumber(112L);
            Assert.IsTrue(shortInfo.isValidShortNumber(sharedEmergencyNumber));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCost(sharedEmergencyNumber));
        }

        [TestMethod] public void testOverlappingNANPANumber()
        {
            // 211 is an emergency number in Barbados, while it is a toll-free information line in Canada
            // and the USA.
            Assert.IsTrue(shortInfo.isEmergencyNumber("211", RegionCode.BB));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                shortInfo.getExpectedCostForRegion("211", RegionCode.BB));
            Assert.IsFalse(shortInfo.isEmergencyNumber("211", RegionCode.US));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion("211", RegionCode.US));
            Assert.IsFalse(shortInfo.isEmergencyNumber("211", RegionCode.CA));
            Assert.AreEqual(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                shortInfo.getExpectedCostForRegion("211", RegionCode.CA));
        }
    }
}
