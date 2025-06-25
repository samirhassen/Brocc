import {defineConfig} from 'vite';
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    base: '/Content/dist/',
    root: './',
    plugins: [tailwindcss()],
    build: {
        outDir: 'dist',
        emptyOutDir: true,
        rollupOptions: {
            input: {
                main: 'src/index.ts',
            },
            output: {
                entryFileNames: 'bundle.min.js',
                assetFileNames: 'bundle.min.[ext]',
            },
        },
    },
});
