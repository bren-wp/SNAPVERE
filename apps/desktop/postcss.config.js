// Tailwind 4 uses a dedicated PostCSS plugin and bundles vendor prefixing.
// Do not register legacy tailwindcss or autoprefixer here.
export default {
  plugins: {
    "@tailwindcss/postcss": {},
  },
};
