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
    public class PhonenumberTest
    {

        [TestMethod]
        public void testEqualSimpleNumber()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setCountryCode(1).setNationalNumber(6502530000L);

            PhoneNumber numberB = new PhoneNumber();
            numberB.setCountryCode(1).setNationalNumber(6502530000L);

            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [TestMethod]
        public void testEqualWithItalianLeadingZeroSetToDefault()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setCountryCode(1).setNationalNumber(6502530000L).setItalianLeadingZero(false);

            PhoneNumber numberB = new PhoneNumber();
            numberB.setCountryCode(1).setNationalNumber(6502530000L);

            // These should still be equal, since the default value for this field is false.
            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [TestMethod]
        public void testEqualWithCountryCodeSourceSet()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setRawInput("+1 650 253 00 00").
                setCountryCodeSource(PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN);
            PhoneNumber numberB = new PhoneNumber();
            numberB.setRawInput("+1 650 253 00 00").
                setCountryCodeSource(PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN);
            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }

        [TestMethod]
        public void testNonEqualWithItalianLeadingZeroSetToTrue()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setCountryCode(1).setNationalNumber(6502530000L).setItalianLeadingZero(true);

            PhoneNumber numberB = new PhoneNumber();
            numberB.setCountryCode(1).setNationalNumber(6502530000L);

            Assert.IsFalse(numberA.Equals(numberB));
            Assert.IsFalse(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [TestMethod]
        public void testNonEqualWithDifferingRawInput()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setCountryCode(1).setNationalNumber(6502530000L).setRawInput("+1 650 253 00 00").
                setCountryCodeSource(PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN);

            PhoneNumber numberB = new PhoneNumber();
            // Although these numbers would pass an isNumberMatch test, they are not considered "equal" as
            // objects, since their raw input is different.
            numberB.setCountryCode(1).setNationalNumber(6502530000L).setRawInput("+1-650-253-00-00").
                setCountryCodeSource(PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN);

            Assert.IsFalse(numberA.Equals(numberB));
            Assert.IsFalse(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [TestMethod]
        public void testNonEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setCountryCode(1).setNationalNumber(6502530000L).setPreferredDomesticCarrierCode("");

            PhoneNumber numberB = new PhoneNumber();
            numberB.setCountryCode(1).setNationalNumber(6502530000L);

            Assert.IsFalse(numberA.Equals(numberB));
            Assert.IsFalse(numberA.GetHashCode() == numberB.GetHashCode());
        }

        [TestMethod]
        public void testEqualWithPreferredDomesticCarrierCodeSetToDefault()
        {
            PhoneNumber numberA = new PhoneNumber();
            numberA.setCountryCode(1).setNationalNumber(6502530000L).setPreferredDomesticCarrierCode("");

            PhoneNumber numberB = new PhoneNumber();
            numberB.setCountryCode(1).setNationalNumber(6502530000L).setPreferredDomesticCarrierCode("");

            Assert.AreEqual(numberA, numberB);
            Assert.AreEqual(numberA.GetHashCode(), numberB.GetHashCode());
        }
    }
}
