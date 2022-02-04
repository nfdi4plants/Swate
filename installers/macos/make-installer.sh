#! /bin/bash

version=$(git describe --tags --abbrev=0 | cut -c2-)
echo "building installer for version ${version}"

rm -f *.pkg

pkgbuild \
    --install-location swate-install \
    --identifier "org.nfdi4plants.Swate" \
    --version "${version}" \
    --scripts scripts \
    --root root \
    swate-pkg.pkg

productbuild \
    --distribution distribution.xml \
    --resources resources \
    Swate.pkg