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
    public static class StringBuilderExtensions
    {
        public static string Substring(this StringBuilder builder, int start, int? end = null)
        {
            if (end == null)
            {
                return builder.ToString(start, builder.Length - start);
            }

            return builder.ToString(start, end.Value - start);
        }

        public static StringBuilder Delete(this StringBuilder builder, int start, int end)
        {
            return builder.Remove(start, end - start);
        }

        public static StringBuilder Replace(this StringBuilder builder, int start, int end, string replacement)
        {
            return builder.Remove(start, end - start).Insert(start, replacement);
        }

        public static int LastIndexOf(this StringBuilder builder, string value)
        {
            return builder.ToString().LastIndexOf(value, StringComparison.Ordinal);
        }
    }
}
