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
using System.Text;

namespace Google.PhoneNumbers
{
    internal static class LeniencyExtensions
    {
        public static bool verify(this PhoneNumberUtil.Leniency leniency, PhoneNumber number, String candidate, PhoneNumberUtil util)
        {
            switch (leniency)
            {
                case PhoneNumberUtil.Leniency.POSSIBLE:
                    return util.isPossibleNumber(number);
                case PhoneNumberUtil.Leniency.VALID:
                {
                    if (!util.isValidNumber(number) ||
                        !PhoneNumberMatcher.containsOnlyValidXChars(number, candidate, util))
                    {
                        return false;
                    }
                    return PhoneNumberMatcher.isNationalPrefixPresentIfRequired(number, util);
                }
                case PhoneNumberUtil.Leniency.STRICT_GROUPING:
                {
                    if (!util.isValidNumber(number) ||
                        !PhoneNumberMatcher.containsOnlyValidXChars(number, candidate, util) ||
                        PhoneNumberMatcher.containsMoreThanOneSlashInNationalNumber(number, candidate) ||
                        !PhoneNumberMatcher.isNationalPrefixPresentIfRequired(number, util))
                    {
                        return false;
                    }
                    return PhoneNumberMatcher.checkNumberGroupingIsValid(number, candidate, util, new StrictGroupingChecker());
                }
                default: //case PhoneNumberUtil.Leniency.EXACT_GROUPING:
                {
                    if (!util.isValidNumber(number) ||
                        !PhoneNumberMatcher.containsOnlyValidXChars(number, candidate, util) ||
                        PhoneNumberMatcher.containsMoreThanOneSlashInNationalNumber(number, candidate) ||
                        !PhoneNumberMatcher.isNationalPrefixPresentIfRequired(number, util))
                    {
                        return false;
                    }
                    return PhoneNumberMatcher.checkNumberGroupingIsValid(number, candidate, util, new ExactGroupingChecker());
                }
            }
        }
    }

    class ExactGroupingChecker : PhoneNumberMatcher.NumberGroupingChecker
    {
        public bool checkGroups(PhoneNumberUtil util, PhoneNumber number,
                                         StringBuilder normalizedCandidate,
                                         String[] expectedNumberGroups) {
                return PhoneNumberMatcher.allNumberGroupsAreExactlyPresent(
                    util, number, normalizedCandidate, expectedNumberGroups);
              }
    }

    class StrictGroupingChecker : PhoneNumberMatcher.NumberGroupingChecker
    {
        public bool checkGroups(PhoneNumberUtil util, PhoneNumber number,
                                        StringBuilder normalizedCandidate,
                                        String[] expectedNumberGroups)
        {
            return PhoneNumberMatcher.allNumberGroupsRemainGrouped(
                util, number, normalizedCandidate, expectedNumberGroups);
        }
    }
}
