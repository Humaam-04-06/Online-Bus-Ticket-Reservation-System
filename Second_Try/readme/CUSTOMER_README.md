# 👤 Customer Portal Documentation

## 📄 Overview
The Customer Portal is a premium traveler's dashboard. It provides a seamless interface for users to book trips, manage their travel history, and customize their personal profile.

---

## ✨ Key Features

### 1. 🏠 Traveler Dashboard
- **Welcome Banner**: Personalized greetings and quick stats (Total Bookings, Notifications).
- **Recent Activity**: Quick view of the latest travel updates.

### 2. 📅 Smart Booking System
- **Split-screen Design**: A modern 60/40 layout for searching and selecting buses.
- **Auto-Fill Logic**: If a user searches for a bus on the public homepage, their selection is automatically carried over to the booking form.
- **Availability Filter**: Search by Origin, Destination, and Travel Date.

### 3. 📋 My Travel Requests
- **Status Tracking**: Live tracking of requests through four states: `Pending`, `Accepted`, `Rejected`, and `Expired`.
- **Filtering**: Quick filters to sort through your travel history.
- **Cancellation**: Ability to cancel pending requests if plans change.

### 4. 🖼️ Profile & Personalization
- **Dynamic Avatars**: Upload and update profile pictures.
- **Banner Customization**: Set a custom cover photo for your profile.
- **Security**: Update account details and passwords.

---

## 🛠️ How It Works
- **Booking Lifecycle**: 
    1. Customer searches and submits a **Request**.
    2. Request status is **Pending**.
    3. Employee accepts → Status becomes **Accepted** (Ticket Generated).
- **Notifications**: A dynamic bell icon in the topbar alerts customers whenever their request status changes.

---

## 📱 Mobile Experience
- **Fluid Layout**: The dashboard adapts perfectly to all screen sizes using Tailwind CSS.
- **Card-Based UI**: Lists like "My Requests" transform into mobile-optimized cards rather than wide tables.
- **Interactive Sidebar**: Easy access to "Book Trip" and "Profile" via a slide-out mobile menu.
