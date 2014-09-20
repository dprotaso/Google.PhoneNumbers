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
    public class RegexCacheTest
    {
        [TestMethod]
        public void testRegexInsertion()
        {
            var regexCache = new RegexCache(2);

            String regex1 = "[1-5]";
            String regex2 = "(?:12|34)";
            String regex3 = "[1-3][58]";

            regexCache.getRegexForRegex(regex1);
            Assert.IsTrue(regexCache.ContainsRegex(regex1));

            regexCache.getRegexForRegex(regex2);
            Assert.IsTrue(regexCache.ContainsRegex(regex2));
            Assert.IsTrue(regexCache.ContainsRegex(regex1));

            regexCache.getRegexForRegex(regex1);
            Assert.IsTrue(regexCache.ContainsRegex(regex1));

            regexCache.getRegexForRegex(regex3);
            Assert.IsTrue(regexCache.ContainsRegex(regex3));

            Assert.IsFalse(regexCache.ContainsRegex(regex2));
            Assert.IsTrue(regexCache.ContainsRegex(regex1));
        }
    }
}
