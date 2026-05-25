# 🛠️ Employee Portal Documentation

## 📄 Overview
The Employee Portal is an operational hub designed for efficiency. Employees use this interface to manage daily booking traffic, verify passengers, and process travel requests in real-time.

---

## ✨ Key Features

### 1. 📈 Operational Dashboard
- **Quick Stats**: At-a-glance view of Today's Bookings and Pending Requests.
- **Task Focus**: Prioritizes urgent customer requests that require immediate attention.

### 2. ⚡ Request Processing
- **Review System**: Detailed view of customer booking details (Route, Date, Bus Type).
- **Filtered Bus Selection**: Dropdowns dynamically filter available active buses by type based on the requested route to ensure operational compatibility.
- **Decision Engine & Validations**: Single-click "Accept" or "Reject" actions, with server-side and client-side validations to ensure positive ticket pricing (`totalFare > 0`).
- **Automation**: Accepting a request automatically generates a formal booking record and notifies the customer.

### 3. 🎫 Digital Ticketing
- **Ticket Generation**: Instant HTML ticket creation for every accepted booking.
- **Print Ready**: Optimized view for printing physical boarding passes for passengers.

### 4. 📜 Booking History
- **Audit Trail**: A complete, searchable log of all past bookings processed by the system.
- **Status Tracking**: Filter by Completed, Accepted, or Rejected statuses.
- **Landscape PDF Export**: Download filtered booking records directly to formatted PDF files for offline reporting.

---

## 🛠️ How It Works
- **Processing Flow**: When a customer submits a request, it appears in the Employee's "Process Request" queue. The employee verifies the availability and clicks accept.
- **Notification Integration**: The system pushes real-time updates to the customer's dashboard once the employee makes a decision.
- **User Interface**: Built with a clean, high-contrast dark theme to reduce eye strain during long shifts.

---

## 📱 Mobile Experience
- **Responsive Grids**: Stat cards stack vertically on smartphones.
- **Sidebar Navigation**: Uses the same advanced mobile-toggle system as the Admin portal, ensuring full functionality on touch devices.
- **Table Accessibility**: Booking logs feature horizontal scroll wrappers for easy review on the go.
