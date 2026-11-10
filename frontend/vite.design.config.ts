import {defineConfig} from 'vite';
import react from '@vitejs/plugin-react';

// Separate entry: no Telegram emulator, backend proxy, or production auth setup.
export default defineConfig({
 plugins:[react()],
 server:{host:'127.0.0.1',port:4173,strictPort:true,open:'/design.html'},
 build:{outDir:'dist-design',rolldownOptions:{input:'design.html'}},
 preview:{host:'127.0.0.1',port:4173,strictPort:true,open:'/design.html'},
});
