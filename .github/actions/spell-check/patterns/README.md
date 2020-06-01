The contents of each `.txt` file in this directory are merged together.
Each line is a Perl 5 regular expression.
Nothing is guaranteed about the order in which they're merged.
-- If this is a problem, please reach out.

Note: order of the contents of these files can matter.
Lines from an individual file are handled in file order.
Files are selected in alphabetical order.

* [n](0_n.txt), [r](0_r.txt), and [t](0_t.txt) are specifically to work around
a quirk in the spell checker:
it often sees C strings of the form "Hello\nwerld". And would prefer to
spot the typo of `werld`.
* [patterns](patterns.txt) is the main list -- there is nothing
particularly special about the file name (beyond the extension which is
important).
