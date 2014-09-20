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

using System.IO;

namespace Google.PhoneNumbers
{
    public static class StreamExtensions
    {
        public static string readUTF(this BinaryReader stream)
        {
            return stream.ReadString();
        }

        public static int readInt(this BinaryReader stream)
        {
            return stream.ReadInt32();
        }

        public static bool readBoolean(this BinaryReader stream)
        {
            return stream.ReadBoolean();
        }


        public static void writeInt(this BinaryWriter stream, int value)
        {
            stream.Write(value);
        }

        public static void writeBoolean(this BinaryWriter stream, bool value)
        {
            stream.Write(value);
        }

        public static void writeUTF(this BinaryWriter stream, string value)
        {
            stream.Write(value);
        }
    }
}
