# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 07/08/2023

### Added

- `Get-PeDependencyChain` have a new parameter `-Depth`. This controls the recursion level for the list.
  Default is zero, which means all. Depth = 1 brings only the dependencies for the main module.

## [1.0.2] - 07/08/2023

Including VC++ redistributables.

## [1.0.1] - 07/08/2023

Empty dependency list bug.

## [1.0.0] - 06/08/2023

First release.
This release includes the development of the CLR core, the main engine
and the Cmdlets.
It also includes the documentation, and module support.
