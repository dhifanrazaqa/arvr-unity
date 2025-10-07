# AR Face Filter dengan Deteksi Gender (AR Foundation + TFLite/REST) 

Aplikasi **Augmented Reality (AR)** berbasis **Unity AR Foundation** yang dapat mendeteksi wajah pengguna menggunakan **AR Face Manager** lalu menampilkan **face filter/prefab** sesuai dengan hasil prediksi **gender**.  
Prediksi gender dapat dijalankan secara **offline** menggunakan **TensorFlow Lite (.tflite)** atau secara **online** melalui **REST API** yang dideploy di **Hugging Face Spaces**.

---

## ✨ Fitur Utama
- 🪞 **AR Face Tracking**: mendeteksi dan melacak wajah pengguna secara real-time.
- 🎭 **Filter AR (Face Prefab)**: tersedia 3 tema filter:
  - 👁️ Eye
  - 👃 Nose
  - 👨🏻‍🦰 Moustache  
  Masing-masing tema memiliki prefab khusus **male** dan **female**.
- 🔄 **AI Mode Toggle**: pengguna dapat mengganti mode prediksi gender:
  - **Offline (Default)** → menggunakan model `.tflite` di device Android.
  - **Online (REST)** → mengirim gambar ke server Flask yang dideploy di Hugging Face.
- 🖱️ **UI Interaktif**:
  - 3 tombol untuk memilih tema filter.
  - Toggle untuk beralih mode AI.
  - Informasi berupa label untuk menampilkan hasil prediksi gender dan confidence score.
- 🌐 **Backend Gratis**: model online dideploy di Hugging Face Spaces dengan Docker container.  
  URL endpoint: [`https://dhifanrazaqa-arvr-gender.hf.space/predict`](https://dhifanrazaqa-arvr-gender.hf.space/predict)

---

## 🏗️ Arsitektur Sistem
### Komponen Utama
- **Unity (Frontend)**
  - `ARFaceManager` → deteksi & tracking wajah.
  - `ThemeManager.cs` → pusat logika:
    - memilih mode AI (offline/online),
    - memanggil `TFLiteRunner.predict()` untuk mode lokal,
    - mengirim request ke REST API untuk mode online,
    - memilih prefab sesuai gender & tema,
    - mengatur UI dan face anchor.
  - `TFLiteRunner.cs` → mengelola inferensi dengan TensorFlow Lite.
  - **Prefabs** → filter untuk male/female (eye, nose, moustache).
- **Backend (Flask + Docker, Hugging Face)**
  - Endpoint `/predict` untuk menerima file gambar, menjalankan prediksi, dan mengembalikan JSON.

### Alur Data
1. AR Camera menangkap wajah → **ARFaceManager** membuat face anchor.
2. Setiap frame diambil oleh **ThemeManager**:
   - Jika **Offline**: memanggil `TFLiteRunner.predict(texture)`.
   - Jika **Online**: mengirim frame ke `/predict`.
3. Backend memproses gambar → hasil prediksi `{label, confidence}`.
4. **ThemeManager** memilih prefab berdasarkan gender dan tema aktif.
5. Filter dipasang pada wajah pengguna secara real-time.

---

## 🤖 Mode AI & Spesifikasi I/O
### 1. Mode Offline (TFLite)
- Model: `model_gender.tflite` (di folder `StreamingAssets`)
- Pre-proses: resize `64×64`, normalisasi 0..1 RGB.
- Output: probabilitas → sigmoid → threshold 0.5 → **Male/Female**.
- Inferensi berkala (200–400 ms) untuk performa optimal.

### 2. Mode Online (REST)
- Endpoint: `POST /predict`
  - Field: `file` (gambar/jpeg)
- Response JSON:
```json
{
  "prediction": "Laki-laki" | "Perempuan",
  "confidence": 0.0-1.0
}
```
- Default Port : 7860 (Di Dockerfile & app.py)
- Deployment : Hugging Face Spaces (containerized) dengan Dockerfile.
Link Space contoh dari tim: https://dhifanrazaqa-arvr-gender.hf.space/predict (jalur /predict).

--- 
## 📁 Struktur Project
```
UnityProject/
 ├─ Assets/
 │   ├─ Scripts/
 │   │   ├─ ThemeManager.cs      # Logika utama aplikasi
 │   │   └─ TFLiteRunner.cs      # Inferensi offline dengan TFLite
 │   ├─ Prefabs/                 # Prefab filter untuk eye, nose, moustache
 │   └─ Scenes/                  # Scene AR (AR Session, Camera, UI)
 │
 └─ Backend/
     ├─ app.py                   # REST API Flask untuk prediksi gender
     ├─ model.h5                 # Model untuk prediksi online
     ├─ requirements.txt         # Dependensi backend
     └─ Dockerfile               # Deployment Hugging Face Spaces
```

---
## 🚀 Cara Menjalankan Aplikasi
### 1. Menjalankan Backend Lokal
```
pip install -r requirements.txt
python app.py
# server di http://0.0.0.0:7860/predict
```

### 2. Deploy Ke Hugging Face Spaces (Container)
1. Buat Space → pilih Docker.
2. Upload folder backend isi apa adanya (Dockerfile, app.py, requirements.txt, model.h5).
3. Tunggu build selesai lalu catat URL Space kamu.
4. Endpoint produksi: https://<space-name>-<username>.hf.space/predict
5. Pastikan di Unity, URL REST diarahkan ke URL Space + /predict.

### 3. Menjalankan Unity (Android)
1. Buka project Unity.
2. Pastikan AR Foundation + ARCore XR Plugin terinstall.
3. Scene: AR Session, AR Session Origin (AR Camera), AR Face Manager, Canvas UI, ThemeManager.
4. Isi referensi di ThemeManager:
    -  drag ARFaceManager,
    - drag TFLiteRunner,
    - isi URL REST ke Space (.../predict).
5. Build Settings → Android: aktifkan ARCore, set Camera permission text, IL2CPP + ARM64.
6. Build & Run ke device Android.

---
## 📜 Lisensi & Atribusi

- AR Foundation/ARCore: lisensi resmi Unity & Google.
- TensorFlow Lite for Unity: sesuai lisensi library terkait.
- Model AI: milik Muhammad Panji Muslim, S.Pd., M.Kom.
- Deployment: Hugging Face Spaces (gratis).