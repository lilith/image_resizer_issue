# image_resizer

these project currently contains three unit tests (using NUnit).

you need to setup the nightly repo for image resizer first (https://www.myget.org/F/imazen-nightlies/api/v2).

when running two of the tests there will be created two files, one good and one bad one (having alpha-blending working and not working).

another test will show two issues in the diagnostics page (missing license and no obvious way to add it and "null pointer exception")

the tests will also emit the execution time which could be a lot faster probably when jpeg encoding / decoding on windows would not be that lame ?!

