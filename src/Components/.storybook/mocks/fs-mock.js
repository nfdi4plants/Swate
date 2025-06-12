export const readFile = async () => {
  throw new Error('fs.readFile is not available in Storybook (mocked)');
};
export default { readFile };