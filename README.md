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

* Thiết bị Android có hỗ trợ nền tảng ARCore.
* Camera hoạt động ổn định.
* GPS và Dịch vụ định vị được bật.
* Kết nối Internet (Wifi/4G) để giao tiếp với AI Backend.
  
## Môi trường phát triển

**1. Unity (Frontend & AR):**
* Unity Editor (Dùng phiên bản **6000.4.7f1** để tránh xung đột).
* Android SDK & NDK (Cài đặt kèm qua Unity Hub).
* AR Foundation & ARCore XR Plugin.
* Firebase SDK.

**2. AI Backend:**
* Python 3.9 trở lên.
* FastAPI (API Server) & Uvicorn.
* [Ollama](https://ollama.com/) (Khởi chạy cục bộ mô hình ngôn ngữ lớn).

---

# Cài đặt dự án

## Clone repository

```bash
git clone https://github.com/Khoi-Tu-2303/ARCampus-HCMUS.git
```
 
## Khởi động AI Backend 
1. **Khởi động Ollama**
   * Mở ứng dụng Ollama trên máy tính.
   * Đảm bảo đã tải mô hình ngôn ngữ lớn được cấu hình trong dự án bằng lệnh:
     ```bash
     ollama run qwen
     ```

2. **Cài đặt môi trường và khởi chạy Server:**
   * Mở một cửa sổ Terminal mới, di chuyển vào thư mục backend của dự án:
     ```bash
     cd backend
     ```
   * Cài đặt toàn bộ các thư viện Python cần thiết được liệt kê trong file cấu hình:
     ```bash
     pip install -r requirements.txt
     ```
   * Khởi chạy API Server bằng Uvicorn:
     ```bash
     uvicorn main:app --host 0.0.0.0 --port 8000 --reload
     ```
---

### Cài đặt và mở dự án Unity

1. **Mở dự án qua Unity Hub:**
   * Khởi động **Unity Hub**.
   * Nhấn vào nút **Add Project** (hoặc Open) ở góc trên bên phải và trỏ trực tiếp đến thư mục dự án đã tải về.
   * Cấu hình phiên bản Editor chính xác là **6000.4.7f1** để đảm bảo tính tương thích tốt nhất cho hệ thống AR Foundation và XR.

2. **Cấu hình dịch vụ Firebase:**
   * Thiết lập các dịch vụ **Firebase Firestore** và **Firebase Authentication** tương ứng trên giao diện điều khiển Firebase Console.
   * Tải tệp cấu hình bảo mật `google-services.json` (dành cho nền tảng Android) từ Firebase về máy.
   * Di chuyển tệp tin này đặt trực tiếp vào thư mục gốc `Assets/` trong cấu trúc Project của Unity để kích hoạt quyền kết nối cơ sở dữ liệu.

3. **Giải quyết các thư viện phụ thuộc (Dependencies):**
   * Để đồng bộ hóa các lớp thư viện Firebase vừa thêm, trên thanh công cụ của Unity Editor, truy cập theo đường dẫn: `Assets > External Dependency Manager > Android Resolver > Force Resolver`. Hệ thống sẽ tự động quét và tải các gói Gradle cần thiết cho Android.
   * **Xử lý lỗi định dạng dữ liệu (Newtonsoft JSON):** Nếu bảng điều khiển Console xuất hiện các lỗi biên dịch liên quan đến việc thiếu không gian tên `Newtonsoft` hoặc `JsonPropertyAttribute`, truy cập vào `Window > Package Manager`, nhấn vào biểu tượng dấu cộng `+`, chọn **Add package by git URL...**, sau đó nhập chuỗi ký tự bên dưới và tải về:
     ```text
     com.unity.nuget.newtonsoft-json
     ```

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
