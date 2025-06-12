export const dirname = (filePath) => {
  return filePath.split('/').slice(0, -1).join('/') || '.';
};

export const join = (...paths) => {
  return paths.flat().join('/').replace(/\/+/g, '/');
};

export default {
  dirname,
  join,
};
