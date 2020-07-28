echo "Installing office addin tooling ..."

npm install -g office-addin-dev-certs
npm install -g office-addin-debugging
npm install -g office-addin-manifest

echo "Starting Excel instance with the SWATE manifest ..."

npx office-addin-debugging start manifest.xml desktop --debug-method web