# ARCampus-HCMUS

Ứng dụng hỗ trợ điều hướng thông minh trong khuôn viên Trường Đại học Khoa học Tự nhiên – ĐHQG TP.HCM bằng công nghệ Thực tế tăng cường (Augmented Reality - AR), kết hợp hệ thống Chatbot AI hỗ trợ tra cứu thông tin và tương tác với người dùng.

![Project Status](https://img.shields.io/badge/Status-Completed-success)
![Unity](https://img.shields.io/badge/Unity-2022+-black)
![C%23](https://img.shields.io/badge/C%23-Language-green)
![AR Foundation](https://img.shields.io/badge/AR-Foundation-blue)
![Firebase](https://img.shields.io/badge/Firebase-Backend-orange)
![Firestore](https://img.shields.io/badge/Firestore-Database-yellow)
![Android](https://img.shields.io/badge/Platform-Android-brightgreen)
![GPS](https://img.shields.io/badge/GPS-Navigation-red)
![Chatbot](https://img.shields.io/badge/AI-Chatbot-purple)
![License](https://img.shields.io/badge/License-Educational-lightgrey)

---

# Giới thiệu

ARCampus-HCMUS là dự án được phát triển nhằm hỗ trợ sinh viên, giảng viên và khách tham quan dễ dàng tìm kiếm địa điểm, định vị, điều hướng và tra cứu thông tin trong khuôn viên trường.

Hệ thống kết hợp nhiều công nghệ hiện đại:

* Augmented Reality (AR)
* GPS & Location Services
* Firebase Firestore
* Indoor Navigation
* Graph-Based Pathfinding
* AI Chatbot Assistant

giúp người dùng tiếp cận thông tin một cách trực quan và thuận tiện hơn.

---

# Chức năng chính

## Xác thực người dùng

* Đăng nhập.
* Đăng ký tài khoản.
* Đăng nhập khách (Guest).
* Phân quyền User và Staff.

## Điều hướng ngoài trời

* Xác định vị trí hiện tại bằng GPS.
* Tìm kiếm địa điểm trong khuôn viên trường.
* Tính toán đường đi tối ưu.
* Hiển thị lộ trình dẫn đường.

## Thực tế tăng cường (AR)

* Hiển thị nhãn AR cho các địa điểm.
* Hiển thị thông tin trực tiếp trên môi trường thực.
* Hỗ trợ định hướng trực quan thông qua camera thiết bị.

## Bản đồ trong nhà

* Hiển thị sơ đồ tầng.
* Tìm kiếm phòng học, đề xuất các địa điểm gần nhất với vị trí hiện tại của người dùng.
* Điều hướng trong các tòa nhà.

## Tra cứu thông tin địa điểm

* Thông tin tòa nhà.
* Thông tin phòng học.
* Thông tin cơ sở vật chất.

## Chatbot AI

* Quản lý hội thoại.
* Lưu lịch sử trò chuyện.
* Hỗ trợ tra cứu thông tin liên quan đến trường.
* Tương tác trực tiếp với người dùng thông qua giao diện chat.

## Chức năng dành cho Staff

* Quản lý thông tin người dùng.
* Cập nhật dữ liệu hệ thống.
* Quản lý nội dung phục vụ chatbot.

---

# Kiến trúc hệ thống

```text
                          ┌───────────────┐
                          │     User      │
                          └───────┬───────┘
                                  │
                                  ▼
                     ┌────────────────────────┐
                     │ Authentication Module  │
                     │ Login / Register       │
                     └──────────┬─────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
                ▼                               ▼
         ┌──────────────┐              ┌──────────────┐
         │ User Account │              │ Staff Account│
         └──────┬───────┘              └──────┬───────┘
                │                             │
                ▼                             ▼
      ┌─────────────────────┐      ┌──────────────────┐
      │     Main Module     │      │ Staff Management │
      └─────┬─────┬─────┬───┘      └──────────────────┘
            │     │     │
            │     │     │
            ▼     ▼     ▼
      ┌────────┐ ┌────────┐ ┌────────────┐
      │ AR     │ │ Indoor │ │ Chatbot    │
      │ Module │ │ Map    │ │ Module     │
      └────┬───┘ └────────┘ └─────┬──────┘
           │                       │
           ▼                       ▼
      ┌─────────────┐     ┌─────────────────┐
      │ Navigation  │     │ Chatbot Backend │
      └──────┬──────┘     └────────┬────────┘
             │                     │
             └─────────┬───────────┘
                       ▼
              ┌──────────────────┐
              │ Firebase Services│
              └──────────────────┘
```

---

# Luồng sử dụng

## Người dùng thông thường

```text
Login
   ↓
Main Scene
   ├── AR Navigation
   ├── Indoor Map
   ├── Search Location
   └── Chatbot
            ↓
   Conversation List
            ↓
        Chat Scene
```

## Nhân viên (Staff)

```text
Login
   ↓
Update Information Scene
```

---

# Công nghệ sử dụng

| Thành phần      | Công nghệ               |
| --------------- | ----------------------- |
| Engine          | Unity                   |
| Ngôn ngữ        | C#                      |
| AR Framework    | AR Foundation           |
| Cơ sở dữ liệu   | Firebase Firestore      |
| Xác thực        | Firebase Authentication |
| Định vị         | GPS                     |
| Điều hướng      | Graph-Based Navigation  |
| Chatbot Backend | REST API                |
| JSON Processing | Newtonsoft.Json         |
| Version Control | Git & GitHub            |

---

# Yêu cầu hệ thống

## Thiết bị

* Android hỗ trợ ARCore.
* Camera hoạt động.
* GPS được bật.
* Kết nối Internet.

## Môi trường phát triển

* Unity
* Android SDK
* Firebase SDK
* AR Foundation

---

# Cài đặt dự án

## Clone repository

```bash
git clone https://github.com/Khoi-Tu-2303/ARCampus-HCMUS.git
```

## Mở dự án

1. Mở Unity Hub.
2. Chọn Add Project.
3. Chọn thư mục dự án.
4. Mở bằng phiên bản Unity được nhóm sử dụng.

## Firebase

Đảm bảo đã cấu hình:

* Firebase Firestore
* Firebase Authentication
* Google Services

theo cấu hình của dự án.

---

# Thành viên nhóm

* Đinh Như Phát 
* Phạm Nguyên Phương
* Từ Văn Khôi
* Trần Lê Minh Đức 
---
## Liên hệ
Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ qua:
- **Email:** 241220**@student.hcmus.edu.vn
- **GitHub:** https://github.com/Khoi-Tu-2303 (Đại diện 1 người)
---
# Giấy phép

Dự án được phát triển phục vụ mục đích học tập, nghiên cứu và ứng dụng công nghệ trong môi trường giáo dục tại trường Đại học Khoa học tự nhiên - ĐHQG TP.HCM
