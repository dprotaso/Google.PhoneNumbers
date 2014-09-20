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

namespace Google.PhoneNumbers
{
    public sealed class PhoneNumberMatch {
  /** The start index into the text. */
  private readonly int _start;
  /** The raw substring matched. */
  private readonly String _rawString;
  /** The matched phone number. */
  private readonly PhoneNumber _number;

  /**
   * Creates a new match.
   *
   * @param start  the start index into the target text
   * @param rawString  the matched substring of the target text
   * @param number  the matched phone number
   */
  internal PhoneNumberMatch(int start, String rawString, PhoneNumber number) {
    if (start < 0) {
      throw new ArgumentException("Start index must be >= 0.");
    }
    if (rawString == null || number == null)
    {
        throw new NullReferenceException();
    }
    this._start = start;
    this._rawString = rawString;
    this._number = number;
  }

  /** Returns the phone number matched by the receiver. */
  public PhoneNumber number() {
    return _number;
  }

  /** Returns the start index of the matched phone number within the searched text. */
  public int start() {
    return _start;
  }

  /** Returns the exclusive end index of the matched phone number within the searched text. */
  public int end() {
    return _start + _rawString.Length;
  }

  /** Returns the raw string matched as a phone number in the searched text. */
  public String rawString() {
    return _rawString;
  }

  public override int GetHashCode() {
    int hash = 41;
    hash = (53 * hash) + _start.GetHashCode();
    hash = (53*hash) + _rawString.GetHashCode();
    hash = (53*hash) + _number.GetHashCode();
    return hash;
  }

  public override bool Equals(Object obj) {
    if (this == obj) {
      return true;
    }
    if (!(obj is PhoneNumberMatch)) {
      return false;
    }
    PhoneNumberMatch other = (PhoneNumberMatch) obj;
    return _rawString.Equals(other._rawString) && (_start == other._start) &&
        _number.Equals(other._number);
  }

  public override String ToString() {
    return "PhoneNumberMatch [" + start() + "," + end() + ") " + _rawString;
  }
}
}
