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
- **Decision Engine**: Single-click "Accept" or "Reject" actions.
- **Seat Conflict Resolution**: Accepting a request auto-cancels other overlapping pending requests requesting the same seats on the same schedule/date.
- **Seat Sync & Release**: If the employee alters seat numbers from the user's initial selection, original requested seats are automatically released so they don't block the seat map.
- **Auto-Calculated Fares**: System locks fare fields and dynamically calculates totals (number of assigned seats * base route fare, minus pre-applied customer voucher discounts). Prevents employee voucher alteration.

### 3. 🎫 Digital Ticketing
- **Ticket Generation**: Instant HTML ticket creation for every accepted booking.
- **Print Ready**: Optimized view for printing physical boarding passes for passengers.

### 4. 📜 Booking History
- **Audit Trail**: A complete, searchable log of all past bookings processed by the system.
- **Status Tracking**: Filter by Completed, Accepted, or Rejected statuses.
- **Landscape PDF Exports**: Download fully structured layout grids of booking logs.

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
