# 🌐 Public Experience Documentation
### (Landing Page, Login & Sign Up)

## 📄 Overview
The public-facing side of SRCTravel is built to WOW users. It combines high-performance animations with a sleek, modern aesthetic to provide the best first impression possible.

---

## ✨ Key Features

### 1. 🚀 Advanced Landing Page
- **GSAP Sticky Stacking**: As you scroll, sections stack on top of each other with a "sticky" effect, creating a premium depth-feel.
- **Interactive Scaling**: Every section scales up (0.6 → 1.0) and fades in as it enters the viewport.
- **Public Search Engine**: A glassmorphic search bar that allows even unregistered users to check bus availability and schedules.
- **Performance Optimized**: Heavy animations are automatically disabled on mobile devices to ensure a smooth 60fps experience.

### 2. 🔐 Dual-Panel Auth System (Login & Sign Up)
- **Urban Nocturne Theme Design**: Deep obsidian panels with electric neon lime green highlights.
- **Sliding Interaction**: A smooth horizontal transition between "Sign In" and "Sign Up" forms on desktop.
- **Intelligent Routing**: The "Sign Up" button in the navbar passes a `?register=true` parameter, which automatically slides the form to the registration panel on page load.
- **Password Strength Meter**: A live, multi-color progress bar that validates password complexity (Weak → Fair → Good → Strong).
- **Dynamic Live Statistics**: Key metrics on the signup panel (fleet size, active routes, customer satisfaction) are fetched dynamically in real-time from the database to present accurate data to prospective users.

### 3. 📱 Mobile-First Adaptation
- **Vertical Stacking**: The complex horizontal sliding animation on the Login page is replaced by a smart vertical stack on mobile.
- **Toggle Switches**: Mobile users get dedicated buttons to switch between "Login" and "Register" views without page reloads.
- **Typography Scaling**: Headings and text sizes automatically scale down for readability on small phone screens.

---

## 🛠️ How It Works
- **GSAP & ScrollTrigger**: The landing page uses the GSAP library to track scroll progress and trigger animations based on the user's position.
- **Tailwind CSS**: Fast, utility-first styling ensures consistent spacing and colors across all public pages.
- **JavaScript Query Handling**: The Login page uses `URLSearchParams` to detect if a user wants to register or log in and adjusts the UI accordingly.

---

## 🎨 Visual Identity
- **Backgrounds**: Deep obsidian black (`#141414`) and charcoal dark surfaces (`#222222`).
- **Accents**: Neon Lime Green (`#E2E800`).
- **Typography**: The **Inter** font family provides a clean, modern, and professional look.
