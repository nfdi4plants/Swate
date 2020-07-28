@ECHO OFF
echo "Installing office addin tooling ..."

call npm install -g office-addin-dev-certs
call npm install -g office-addin-debugging
call npm install -g office-addin-manifest

echo "Starting Excel instance with the SWATE manifest ..."

npx office-addin-debugging start manifest.xml desktop --debug-method web