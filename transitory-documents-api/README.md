# Transitory Documents API Configuration Notes

The standard appsettings values should be applicable to most environments. See below for additional context on the purpose of key sections.

## Keycloak.QueryRole

Simple keycloak role auth required to support querying for transitory documents.

## Keycloak.ReadRole

Simple keycloak role auth required to support downloading transitory document contents.

## CorrectionMappings

Mappings are handled by this api instead of the client due to the abstract purpose of the transitory-documents-api. The caller (ie. JASPER) does not know where or how the transitive documents are stored, just that this api gates access. Therefore, the knowledge that the documents are stored using SMB, and the specific knowledge of the individual (corrected) names of regions and locations within that smb are only available within this api.

Note that these mappings are not exhaustive, they are only used when the standard region name or location name provided by the jc-interface/PCSS do not match what is being used in the shared drive. When no mapping is required, the value provided by the caller is used.

Stable region and location identifiers are used as the source in these mappings (ie. instead of the region/location name) to avoid a region/location rename or reordering from negatively affecting this api.

Note that the SMBLibrary cannot ignore case-sensitivity without performance implications. In order to do case-insensitive querying of region/location/date, the api would first have to query the base directory, and list all folders. This would allow case-insensitive querying of the region. Next the api would query the base/region for all folders which would allow case-insensitive querying of the region and the location. Given that every directory query is a full round-trip from this api to the SMB Drive, we use a case-sensitive search of the initial region/location/date for performance.

## CorrectionMappings.RegionMappings

Use these mappings to remap PCSS/JC region to the non-standard region names used on the P/Q drive. Source is the region id from PCSS. If no matching mapping is detected, use the passed region name.

## CorrectionMappings.LocationMappings

Use these mappings to remap PCSS/JC location to the non-standard (or satellite) location names used on the P/Q drive. Source is the agencyIdentiferCd from the jc-interface (presumed to be more stable then the PCSS location id). If no matching mapping is detected, use the passed location name.

Note that satellite locations will map a single location agency id to multiple folders, ie. 4711 to Cranbrook/Cranbrook (compared to a standard, non-satellite mapping: 5941 to STEWART).

## SharedDrive.DateFolderFormats

`DateFolderFormats` allows multiple date folder patterns so we can handle inconsistent folder structures (for example, `"MM MMMM/MMMM d*"`). Add additional patterns here as new naming conventions surface. Note that adding multiple expected date folder patterns will have a performance impact due to the underlying implementation of the SMBLibrary.

## SharedDrive.RoomTraversalMaxConcurrency

This parameter controls the number of threads allowed to access different room folders nested under the target region/location/date. When a room is not specified, the SMBService needs to access all room folders underneath the target. This is handled via multiple threads to speed up the querying process. Increasing this number may increase performance, but may also increase the risk of the SMB drive blocking some concurrent network calls. The current number is based on recommendations provided by the SMBLibrary.
