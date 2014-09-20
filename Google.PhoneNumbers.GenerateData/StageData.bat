

.\bin\StageData\Google.PhoneNumbers.GenerateData.exe ^
  --input-file=%cd%\Data\PhoneNumberMetadata.xml ^
  --output-dir=%cd%\..\Google.PhoneNumbers ^
  --data-prefix=Data\PhoneNumberMetadataProto ^
  --mapping-class=CountryCodeToRegionCodeMap ^
  --copyright=2014 ^
  --lite-build=false

.\bin\StageData\Google.PhoneNumbers.GenerateData.exe ^
  --input-file=%cd%\Data\PhoneNumberAlternateFormats.xml ^
  --output-dir=%cd%\..\Google.PhoneNumbers ^
  --data-prefix=Data\PhoneNumberAlternateFormatsProto ^
  --mapping-class=AlternateFormatsCountryCodeSet ^
  --copyright=2014 ^
  --lite-build=false

.\bin\StageData\Google.PhoneNumbers.GenerateData.exe ^
  --input-file=%cd%\Data\ShortNumberMetadata.xml ^
  --output-dir=%cd%\..\Google.PhoneNumbers ^
  --data-prefix=Data\ShortNumberMetadataProto ^
  --mapping-class=ShortNumbersRegionCodeSet ^
  --copyright=2014 ^
  --lite-build=false

.\bin\StageData\Google.PhoneNumbers.GenerateData.exe ^
  --input-file=%cd%\Data\PhoneNumberMetadataForTesting.xml ^
  --output-dir=%cd%\..\Google.PhoneNumbers.Test ^
  --data-prefix=Data\PhoneNumberMetadataProtoForTesting ^
  --mapping-class=CountryCodeToRegionCodeMapForTesting ^
  --copyright=2014 ^
  --lite-build=false