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

using System.Text;
using System.Text.RegularExpressions;

namespace Google.PhoneNumbers
{
    internal class JavaRegex : Regex
    {
        private readonly Regex _lookingAt;
        private readonly Regex _matches;

        public JavaRegex(string regex, RegexOptions options = RegexOptions.None) 
            : base(regex, options)
        {
            var updatedRegex = @"\A(?:" + regex + @")\z";
            _matches = new Regex(updatedRegex, options);

            updatedRegex = @"\A(?:"+ regex + ")";
            _lookingAt = new Regex(updatedRegex, options);
        }

        public Match MatchBeginning(string input)
        {
            return _lookingAt.Match(input);
        }

        public Match MatchWhole(string input)
        {
            return _matches.Match(input);
        }

        public Match MatchBeginning(StringBuilder input)
        {
            return _lookingAt.Match(input.ToString());
        }

        public Match MatchWhole(StringBuilder input)
        {
            return _matches.Match(input.ToString());
        }

        public string Replace(string input, StringBuilder replacement, int i)
        {
            return Replace(input, replacement.ToString(), i);
        }
    }
}
