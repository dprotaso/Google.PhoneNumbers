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
  public class PhoneNumber  {
    public enum CountryCodeSource {
      FROM_NUMBER_WITH_PLUS_SIGN,
      FROM_NUMBER_WITH_IDD,
      FROM_NUMBER_WITHOUT_PLUS_SIGN,
      FROM_DEFAULT_COUNTRY
    }

    public PhoneNumber() {
      countryCodeSource_ = CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN;
    }

    // required int32 country_code = 1;
    private bool hasCountryCode;
    private int countryCode_ = 0;
    public bool HasCountryCode() { return hasCountryCode; }
    public int getCountryCode() { return countryCode_; }
    public PhoneNumber setCountryCode(int value) {
      hasCountryCode = true;
      countryCode_ = value;
      return this;
    }
    public PhoneNumber clearCountryCode() {
      hasCountryCode = false;
      countryCode_ = 0;
      return this;
    }

    // required uint64 national_number = 2;
    private bool hasNationalNumber;
    private long nationalNumber_ = 0L;
    public bool HasNationalNumber() { return hasNationalNumber; }
    public long getNationalNumber() { return nationalNumber_; }
    public PhoneNumber setNationalNumber(long value) {
      hasNationalNumber = true;
      nationalNumber_ = value;
      return this;
    }
    public PhoneNumber clearNationalNumber() {
      hasNationalNumber = false;
      nationalNumber_ = 0L;
      return this;
    }

    // optional string extension = 3;
    private bool hasExtension;
    private string extension_ = "";
    public bool HasExtension() { return hasExtension; }
    public String getExtension() { return extension_; }
    public PhoneNumber setExtension(String value) {
      if (value == null) {
        throw new NullReferenceException();
      }
      hasExtension = true;
      extension_ = value;
      return this;
    }
    public PhoneNumber clearExtension() {
      hasExtension = false;
      extension_ = "";
      return this;
    }

    // optional bool italian_leading_zero = 4;
    private bool hasItalianLeadingZero;
    private bool italianLeadingZero_ = false;
    public bool HasItalianLeadingZero() { return hasItalianLeadingZero; }
    public bool isItalianLeadingZero() { return italianLeadingZero_; }
    public PhoneNumber setItalianLeadingZero(bool value) {
      hasItalianLeadingZero = true;
      italianLeadingZero_ = value;
      return this;
    }
    public PhoneNumber clearItalianLeadingZero() {
      hasItalianLeadingZero = false;
      italianLeadingZero_ = false;
      return this;
    }

    // optional int32 number_of_leading_zeros = 8 [default = 1];
    private bool hasNumberOfLeadingZeros;
    private int numberOfLeadingZeros_ = 1;
    public bool HasNumberOfLeadingZeros() { return hasNumberOfLeadingZeros; }
    public int getNumberOfLeadingZeros() { return numberOfLeadingZeros_; }
    public PhoneNumber setNumberOfLeadingZeros(int value) {
      hasNumberOfLeadingZeros = true;
      numberOfLeadingZeros_ = value;
      return this;
    }
    public PhoneNumber clearNumberOfLeadingZeros() {
      hasNumberOfLeadingZeros = false;
      numberOfLeadingZeros_ = 1;
      return this;
    }

    // optional string raw_input = 5;
    private bool hasRawInput;
    private String rawInput_ = "";
    public bool HasRawInput() { return hasRawInput; }
    public String getRawInput() { return rawInput_; }
    public PhoneNumber setRawInput(String value) {
      if (value == null) {
        throw new NullReferenceException();
      }
      hasRawInput = true;
      rawInput_ = value;
      return this;
    }
    public PhoneNumber clearRawInput() {
      hasRawInput = false;
      rawInput_ = "";
      return this;
    }

    // optional CountryCodeSource country_code_source = 6;
    private bool hasCountryCodeSource;
    private CountryCodeSource countryCodeSource_;
    public bool HasCountryCodeSource() { return hasCountryCodeSource; }
    public CountryCodeSource getCountryCodeSource() { return countryCodeSource_; }
    public PhoneNumber setCountryCodeSource(CountryCodeSource? value) {
      if (value == null) {
        throw new NullReferenceException();
      }
      hasCountryCodeSource = true;
      countryCodeSource_ = value.Value;
      return this;
    }
    public PhoneNumber clearCountryCodeSource() {
      hasCountryCodeSource = false;
      countryCodeSource_ = CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN;
      return this;
    }

    // optional string preferred_domestic_carrier_code = 7;
    private bool hasPreferredDomesticCarrierCode;
    private string preferredDomesticCarrierCode_ = "";
    public bool HasPreferredDomesticCarrierCode() { return hasPreferredDomesticCarrierCode; }
    public String getPreferredDomesticCarrierCode() { return preferredDomesticCarrierCode_; }
    public PhoneNumber setPreferredDomesticCarrierCode(String value) {
      if (value == null) {
        throw new NullReferenceException();
      }
      hasPreferredDomesticCarrierCode = true;
      preferredDomesticCarrierCode_ = value;
      return this;
    }
    public PhoneNumber clearPreferredDomesticCarrierCode() {
      hasPreferredDomesticCarrierCode = false;
      preferredDomesticCarrierCode_ = "";
      return this;
    }

    public PhoneNumber clear() {
      clearCountryCode();
      clearNationalNumber();
      clearExtension();
      clearItalianLeadingZero();
      clearNumberOfLeadingZeros();
      clearRawInput();
      clearCountryCodeSource();
      clearPreferredDomesticCarrierCode();
      return this;
    }

    public PhoneNumber mergeFrom(PhoneNumber other) {
      if (other.HasCountryCode()) {
        setCountryCode(other.getCountryCode());
      }
      if (other.HasNationalNumber()) {
        setNationalNumber(other.getNationalNumber());
      }
      if (other.HasExtension()) {
        setExtension(other.getExtension());
      }
      if (other.HasItalianLeadingZero()) {
        setItalianLeadingZero(other.isItalianLeadingZero());
      }
      if (other.HasNumberOfLeadingZeros()) {
        setNumberOfLeadingZeros(other.getNumberOfLeadingZeros());
      }
      if (other.HasRawInput()) {
        setRawInput(other.getRawInput());
      }
      if (other.HasCountryCodeSource()) {
        setCountryCodeSource(other.getCountryCodeSource());
      }
      if (other.HasPreferredDomesticCarrierCode()) {
        setPreferredDomesticCarrierCode(other.getPreferredDomesticCarrierCode());
      }
      return this;
    }

    public bool exactlySameAs(PhoneNumber other) {
      if (other == null) {
        return false;
      }
      if (this == other) {
        return true;
      }
      return (countryCode_ == other.countryCode_ && nationalNumber_ == other.nationalNumber_ &&
          extension_.Equals(other.extension_) && italianLeadingZero_ == other.italianLeadingZero_ &&
          numberOfLeadingZeros_ == other.numberOfLeadingZeros_ &&
          rawInput_.Equals(other.rawInput_) && countryCodeSource_ == other.countryCodeSource_ &&
          preferredDomesticCarrierCode_.Equals(other.preferredDomesticCarrierCode_) &&
          HasPreferredDomesticCarrierCode() == other.HasPreferredDomesticCarrierCode());
    }

    public override bool Equals(object that) {
      return (that is PhoneNumber) && exactlySameAs((PhoneNumber) that);
    }

    public override int GetHashCode() {
      // Simplified rendition of the hashCode function automatically generated from the proto
      // compiler with java_generate_equals_and_hash set to true. We are happy with unset values to
      // be considered equal to their explicitly-set equivalents, so don't check if any value is
      // unknown. The only exception to this is the preferred domestic carrier code.
      int hash = 41;
      hash = (53 * hash) + getCountryCode();
      hash = (53 * hash) + getNationalNumber().GetHashCode();
      hash = (53 * hash) + getExtension().GetHashCode();
      hash = (53 * hash) + (isItalianLeadingZero() ? 1231 : 1237);
      hash = (53 * hash) + getNumberOfLeadingZeros();
      hash = (53 * hash) + getRawInput().GetHashCode();
      hash = (53 * hash) + getCountryCodeSource().GetHashCode();
      hash = (53 * hash) + getPreferredDomesticCarrierCode().GetHashCode();
      hash = (53 * hash) + (HasPreferredDomesticCarrierCode() ? 1231 : 1237);
      return hash;
    }

    public override String ToString() {
      StringBuilder outputString = new StringBuilder();
      outputString.Append("Country Code: ").Append(countryCode_);
      outputString.Append(" National Number: ").Append(nationalNumber_);
      if (HasItalianLeadingZero() && isItalianLeadingZero()) {
        outputString.Append(" Leading Zero(s): true");
      }
      if (HasNumberOfLeadingZeros()) {
        outputString.Append(" Number of leading zeros: ").Append(numberOfLeadingZeros_);
      }
      if (HasExtension()) {
        outputString.Append(" Extension: ").Append(extension_);
      }
      if (HasCountryCodeSource()) {
        outputString.Append(" Country Code Source: ").Append(countryCodeSource_);
      }
      if (HasPreferredDomesticCarrierCode()) {
        outputString.Append(" Preferred Domestic Carrier Code: ").
            Append(preferredDomesticCarrierCode_);
      }
      return outputString.ToString();
    }
  }
}