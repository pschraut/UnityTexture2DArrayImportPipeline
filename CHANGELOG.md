# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.4.0] - 202?-??-??
After installing this update, it will trigger a reimport of all Texture2DArray assets in the project and Texture2DArray's will no longer be readable via scripts by default.
### Added
 - Added ability to toggle whether the Texture2DArray is readable from scripts at the expense of consuming more memory when turned on. The default is off. Thanks to Guarneri1743 for the contribution, see [PR#7](https://github.com/pschraut/UnityTexture2DArrayImportPipeline/pull/7).

### Changed
 - Creating a new Texture2DArray asset will now no longer be readable from scripts by default. If you want to restore the previous behavior, you need to enable the ```Read/Write Enabled``` option.


## [1.3.0] - 2021-02-21
After installing this update, it will trigger a reimport of all Texture2DArray assets in the project.
### Fixed 
 - Fixed Texture2DArray not updating its texture format when changing the build target with [Asset Import Pipeline V2](https://blogs.unity3d.com/2019/10/31/the-new-asset-import-pipeline-solid-foundation-for-speeding-up-asset-imports/) being used. Thanks to Bastien of Unity Technologies for the help (actually providing the fix/workaround). This should solve [issue #3](https://github.com/pschraut/UnityTexture2DArrayImportPipeline/issues/3).


## [1.2.0] - 2020-11-02
### Fixed 
 - Fixed compile error in Unity 2020.2 (ScriptedImporter was moved to a different namespace)
 - Don't display the Texture2DArray imported object twice in the Inspector


## [1.1.0] - 2020-08-29
### Changed
 - Removed "partial" keyword from the Texture2DArrayImporter class.
 
### Fixed
 - Added missing reference Unity TestRunner assemblies to Texture2D Import Pipeline test assembly.
 - Various documentation fixes.
 
## [1.0.0] - 2019-09-02
 - First release
