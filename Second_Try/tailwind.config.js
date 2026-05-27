/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        primary: 'var(--bg-primary)',
        secondary: 'var(--bg-secondary)',
        cyan: 'var(--accent-cyan)',
        purple: 'var(--accent-purple)',
        blue: 'var(--accent-blue)',
        light: 'var(--text-light)',
        muted: 'var(--text-muted)',
        border: 'var(--border-color)',
        success: 'var(--success)',
        danger: 'var(--danger)'
      },
      fontFamily: {
        sans: ['Inter', 'sans-serif'],
      }
    },
  },
  plugins: [],
}
