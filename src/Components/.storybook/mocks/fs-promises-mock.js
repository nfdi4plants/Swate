export const stat = async () => {
  throw new Error('fs.promises.stat is mocked for Storybook.');
};

export const mkdir = async () => {
  throw new Error('fs.promises.mkdir is mocked for Storybook.');
};

export const readdir = async () => {
  throw new Error('fs.promises.readdir is mocked for Storybook.');
};

export const rename = async () => {
  throw new Error('fs.promises.rename is mocked for Storybook.');
};

export const unlink = async () => {
  throw new Error('fs.promises.unlink is mocked for Storybook.');
};

export const rm = async () => {
  throw new Error('fs.promises.rm is mocked for Storybook.');
};

export const readFile = async () => {
  throw new Error('fs.promises.readFile is mocked for Storybook.');
};

export const writeFile = async () => {
  throw new Error('fs.promises.writeFile is mocked for Storybook.');
};

export default {
  stat,
  mkdir,
  readdir,
  rename,
  unlink,
  rm,
  readFile,
  writeFile,
};
