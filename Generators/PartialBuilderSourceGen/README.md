# Partial Builder Source Gen

**Key Concepts:**

1. A property should be required if it's not nullable.
1. If the type being built is a class, use a class updater.
1. If the type being built is a struct, use a struct updater.
	1. If `struct`, `AppendUpdate(ref T, out ...Changes)` will be used instead of `void AppendUpdate(T, out ...Changes)`.
	1. If `readonly struct`, `T WithUpdate(T, out ...Changes)` will be used instead of `void AppendUpdate(T, out ...Changes)`. 
1. Changes which each update method expose should be a `struct ...Changes` which expresses values added, updated or removed.
	1. Changes to a `Dictionary` changes should include `Added`, `Removed`, and `Updated`.
	1. Changes to a `Set` should include `Added` and `Removed`.
	1. Changes to a regular value should include `...HasChanged` and `...Previous` to store the value before the update.
	1. Changes to an updater 