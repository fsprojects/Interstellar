# Contributing to Interstellar

High-quality contributions are very much welcome. Pull requests should do one thing only and do it well; if you have multiple separate contributions, please split them
into individual, independent PRs, or they may not be accepted. A good PR might consist of one of these (but is not limited to these):

* A bug fix
* A feature implementation
* Code cleanup (with reasonable scope)
* Documentation improvement (again, with reasonable scope)

## Testing

Interstellar doesn't have any unit tests because it's generally too costly and impractical to unit test GUI systems. If anyone thinks they have some good tests to add
to prove me wrong, certainly submit a PR and I will reconsider! End-to-end tests may be a good idea; i.e. tests that just make sure that the app starts up and a window
appears would probably be prudent.

Therefore, you must manually test your changes by running the example apps included in the solution files.

## Releasing

To create a new release version, add a new section to [CHANGELOG.md](CHANGELOG.md). The build system uses the first section in the file to determine the version number
as well as the release notes.

Packaging a release is as easy as running ``dotnet fake build -t PackAll`` (they will end up in the ``artifacts/`` directory). CI will always attempt to build the
NuGet packages, but does not yet automatically publish to NuGet, so to publish a new release, you must download the built packages from the GitHub CI and then manually
publish (see https://github.com/jwosty/Interstellar/issues/16).

After releasing the packages to NuGet, update the templates (found under templates/) to use the new package versions, and then release those.
