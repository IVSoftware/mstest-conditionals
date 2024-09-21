Your comment states:

> "System prerequisites not met" is discovered at runtime.

But you also make reference to the `[Ignore]` attribute and also mention `#if`.

> I could try comment or #ifdeffing the TestMethod attribute to sidestep the issue, rather than adding the Ignored attribute. 

The second approach is at odds with the first. Attributes like `[Ignore]` are evaluated at compile time and can't be dynamically altered once the code is running. Conditional compilation (`#if`) is also a compile-time feature. This, too, is inherently static and does not allow for changes during runtime. For example, the image below shows the immediate effect of unchecking a custom conditional compile symbol from the project's properties window.

[Image placeholder]





