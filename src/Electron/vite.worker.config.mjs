import { defineConfig } from 'vite';

const entry = 'src/fable_output/Main/Workers/TextDiffWorkerEntry.fs.jsx';

// The text diff worker bundle. The Forge plugin keeps Node built-ins and electron
// external, everything else goes into this one CommonJS file.
export default defineConfig({
  build: {
    outDir: '.vite/build',
    emptyOutDir: false,
    lib: {
      entry,
      formats: ['cjs'],
      fileName: () => 'text-diff-worker.cjs',
    },
    rollupOptions: {
      output: {
        inlineDynamicImports: true,
      },
    },
  },
});
