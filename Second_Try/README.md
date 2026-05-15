# SRCTravel — Modern Online Bus Ticket Reservation System

## 🚀 Current Project Status
The system is in an advanced state of development. The core portals (Admin, Employee, Customer) are fully operational. We have successfully integrated an AI Chatbot and completed **Phase 1** of our advanced ticketing roadmap (Bus Schedules & Public Search).

---

## 🎨 UI / UX Design System
- **Theme**: Urban Nocturne (Premium Dark Mode)
- **Typography**: Inter (Google Fonts)
- **Primary Colors**: Cyan (`#00D1FF`), Blue (`#007BFF`), Purple (`#8A2BE2`)
- **Style**: Glassmorphism, blurred panels, neon glow accents.

---

## ✅ Completed Modules

### 1. Authentication & Security
- Cookie-based Auth with `[Authorize]` role checks (Admin, Employee, Customer).
- BCrypt password hashing.
- Password strength meters and CSRF protection (`[ValidateAntiForgeryToken]`).

### 2. Admin Portal
- **Manage Employees:** Create, view, and block employee accounts.
- **Manage Buses:** Configure Standard, Luxury, Express, Economy fleets.
- **Manage Routes & Prices:** Set up origin/destination routes and class-based fare lists.
- **Manage Schedules:** Create daily/recurring `BusSchedule` entries with specific Departure and Arrival times.
- **Reports & History:** View total revenue, monthly trends, and global booking history.

### 3. Employee Portal
- **Dashboard:** Live active booking requests and today's operations.
- **Process Requests:** Accept or Reject customer requests (automatically creates `Booking` and `Notification` records).
- **Print Ticket:** HTML ticket view for accepted bookings.

### 4. Customer Portal
- **Dashboard:** Personal stats, active requests, and a dynamic notification bell.
- **New Request:** Modern 60/40 split-screen booking form. Automatically pre-fills data if initiated from the Public Search Engine.
- **My Requests:** Track all Pending, Accepted, Rejected, and Cancelled requests.
- **Profile:** Upload profile pictures and banner images dynamically.

### 5. ARIA AI Chatbot (Gemini Integration)
- Floating, interactive AI assistant injected into all layouts (`_AdminLayout`, `_EmployeeLayout`, `_CustomerLayout`, `_PublicLayout`).
- Powered by Google's `gemini-3-flash-preview` model.
- Features multi-key round-robin failover to prevent rate limits.
- Context-aware: It knows if you are logged in and can retrieve your personal booking history to answer specific questions.

### 6. Background Services
- `RequestExpiryService`: Runs silently every hour to automatically cancel `Pending` requests if the `TravelDate` has already passed, keeping the database clean.

### 7. Public Search Engine (Phase 1 of Roadmap)
- Modern, animated homepage with a glassmorphic Search Bar.
- Unregistered users can search routes and dates.
- Search results show available `BusSchedules`, bus classes, departure/arrival times, and fares.
- Clicking "Book Now" locks in the specific schedule and routes the user to the Customer Portal.

---

## 🗺️ Master Roadmap (Remaining Work)

We are currently tracking against a 4-Phase advanced upgrade plan:

| Phase | Status | Description |
|---|---|---|
| **Phase 1** | ✅ COMPLETE | **Schedules & Public Search:** Created `BusSchedule` entity, Admin CRUD, and Public Homepage Search API. |
| **Phase 2** | ⏳ UP NEXT | **Interactive Seat Selection:** A modern CSS grid showing the physical bus layout. Customers will click specific seats (e.g. 1A, 2B) preventing double-booking. |
| **Phase 3** | 📅 PLANNED | **PDF Ticket Generation:** Use a C# library to generate a formal, downloadable PDF boarding pass with a scannable QR code. |
| **Phase 4** | 📅 PLANNED | **Customer Reviews & Ratings:** Add `Completed` status so employees can mark who actually boarded the bus. Only verified passengers can leave a star rating displayed on the homepage. |

---

*This README is continuously updated as the project progresses.*
