# SRCTravel — Modern Online Bus Ticket Reservation System

## 🚀 Current Project Status: **Full System Responsiveness & Advanced UI**
The system has reached a critical milestone. We have achieved **100% Mobile Responsiveness** across every module, from the public landing page to the deep admin dashboards. The UI now features state-of-the-art **GSAP Scroll-Triggered Animations** and a premium glassmorphic aesthetic that adapts fluidly to any device size.

---

## 🎨 UI / UX Design System (Premium Experience)
- **Theme**: Urban Nocturne (Premium Dark Mode)
- **Design Philosophy**: Glassmorphism, blurred panels, and neon glow accents.
- **Animations**: 
    - **GSAP Stacking**: Section-on-section stacking effect on the landing page.
    - **Scroll Scaling**: Interactive section scaling (0.6 → 1.0) as users explore.
    - **Micro-interactions**: Hover effects, smooth transitions, and loading skeletons.
- **Responsiveness**: Mobile-first architecture using Tailwind CSS and targeted Media Queries.

---

## ✅ Core Features & Completed Modules

### 1. 📱 Universal Responsiveness
- **Adaptive Dashboards**: Grid systems for Admin, Employee, and Customer portals automatically reconfigure (4-col → 2-col → 1-col).
- **Mobile Navigation**: Slide-out sidebars with glassmorphic overlays for portal management on the go.
- **Touch-Friendly Tables**: Horizontal scroll wrappers for all data tables, ensuring usability on small viewports.
- **Responsive Auth**: Sliding login/register panels stack vertically on mobile with dedicated toggle switches.

### 2. 🔐 Authentication & Security
- **Dynamic Auth UX**: Navbar "Sign Up" button intelligently routes to the registration panel via `?register=true` query parameters.
- **Security**: BCrypt hashing, CSRF protection, and role-based access control (`Admin`, `Employee`, `Customer`).
- **Validation**: Real-time password strength meter with visual feedback and submission blocking.

### 3. 🏢 Admin Portal (The Command Center)
- **Fleet Management**: Configure and track various bus classes (Standard, Luxury, etc.).
- **Operations Control**: Manage Routes, Prices, and detailed Bus Schedules.
- **Personnel Management**: Full CRUD and blocking capabilities for Employee accounts.
- **Global Insights**: High-level stats, revenue reports, and booking trends.

### 4. 🛠️ Employee Portal (Field Operations)
- **Request Processing**: Accept or Reject customer booking requests with real-time feedback.
- **Ticketing**: Instant HTML ticket generation for confirmed passengers.
- **Active Tracking**: Dashboard for today's departures and pending tasks.

### 5. 👤 Customer Portal (Traveler Experience)
- **Smart Booking**: 60/40 split-screen booking form with auto-fill from public search.
- **Request Tracking**: Categorized list of all travel requests (Pending, Accepted, Rejected, Expired).
- **Dynamic Profiles**: Integrated profile and banner image management.

### 6. 🤖 ARIA AI Chatbot (Gemini Integration)
- **Hyper-Contextual**: Directly integrated with the database to answer questions about *your* specific bookings.
- **Advanced Failover**: Round-robin API key management for 100% uptime.
- **Universal Presence**: Accessible from every page in the application.

---

## 🗺️ Master Roadmap

| Phase | Status | Description |
|---|---|---|
| **Phase 1** | ✅ COMPLETE | **Schedules & Public Search:** Core search engine and backend scheduling logic. |
| **Phase 2** | ✅ COMPLETE | **Mobile UX & UI Overhaul:** GSAP animations, 100% responsiveness, and premium design polish. |
| **Phase 3** | ⏳ UP NEXT | **Interactive Seat Selection:** Physical bus layout grid for seat-specific bookings. |
| **Phase 4** | 📅 PLANNED | **PDF & QR Ticketing:** Formal downloadable boarding passes with QR validation. |
| **Phase 5** | 📅 PLANNED | **Review System:** Verified passenger ratings and star reviews. |

---

*This project is built with ASP.NET Core MVC, Entity Framework Core, and a heavy emphasis on modern Front-end excellence.*
