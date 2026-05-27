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

## 📚 Detailed Module Documentation

For an in-depth look at each specific part of the system, please refer to the dedicated documentation files:

- 👑 **[Admin Portal Documentation](./readme/ADMIN_README.md)**: Fleet management, personnel CRUD, reports, and global controls.
- 🛠️ **[Employee Portal Documentation](./readme/EMPLOYEE_README.md)**: Operational processing, ticket generation, and booking history.
- 👤 **[Customer Portal Documentation](./readme/CUSTOMER_README.md)**: Smart booking, status tracking, notifications, and profile management.
- 🌐 **[Public Experience & Auth Documentation](./readme/PUBLIC_PAGES_README.md)**: GSAP animations, landing page search, and the dual-panel auth system.


*This project is built with ASP.NET Core MVC, Entity Framework Core, and a heavy emphasis on modern Front-end excellence.*
