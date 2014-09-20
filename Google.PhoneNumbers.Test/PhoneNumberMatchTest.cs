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
    public class PhoneNumberMatchTest
    {
        /**
   * Tests the value type semantics. Equality and hash code must be based on the covered range and
   * corresponding phone number. Range and number correctness are tested by
   * {@link PhoneNumberMatcherTest}.
   */
        [TestMethod]
        public void testValueTypeSemantics()
        {
            PhoneNumber number = new PhoneNumber();
            PhoneNumberMatch match1 = new PhoneNumberMatch(10, "1 800 234 45 67", number);
            PhoneNumberMatch match2 = new PhoneNumberMatch(10, "1 800 234 45 67", number);

            Assert.AreEqual(match1, match2);
            Assert.AreEqual(match1.GetHashCode(), match2.GetHashCode());
            Assert.AreEqual(match1.start(), match2.start());
            Assert.AreEqual(match1.end(), match2.end());
            Assert.AreEqual(match1.number(), match2.number());
            Assert.AreEqual(match1.rawString(), match2.rawString());
            Assert.AreEqual("1 800 234 45 67", match1.rawString());
        }

        /**
   * Tests the value type semantics for matches with a null number.
   */

        [TestMethod]
        public void testIllegalArguments()
        {
            try
            {
                new PhoneNumberMatch(-110, "1 800 234 45 67", new PhoneNumber());
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                /* success */
            }

            try
            {
                new PhoneNumberMatch(10, "1 800 234 45 67", null);
                Assert.Fail();
            }
            catch (NullReferenceException)
            {
                /* success */
            }

            try
            {
                new PhoneNumberMatch(10, null, new PhoneNumber());
                Assert.Fail();
            }
            catch (NullReferenceException)
            {
                /* success */
            }

            try
            {
                new PhoneNumberMatch(10, null, null);
                Assert.Fail();
            }
            catch (NullReferenceException)
            {
                /* success */
            }
        }
    }
}
