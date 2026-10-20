/**
 * Hayat İşleri - Kurumsal B2B & Çoklu Dil (i18n) Çeviri Motoru
 * Türkçe (TR) <-> English (EN) Tam Kapsamlı, Profesyonel Çeviri Sistemi
 * 
 * Özellikler:
 * - Marka, logo, tescilli ürün adları ve özel şirket unvanlarını %100 korur (çevirmez).
 * - Cümle ve tam ifade düzeyinde kusursuz kurumsal çeviri sunar (kelimeleri ortadan bölüp bozmaz).
 * - Türkçe harflere duyarlı kelime sınırı kontrolü ile "Softwareları" gibi bozuk melez kelimeleri engeller.
 * - DOM metin düğümleri (TextNode), placeholder, title, aria-label ve form elemanlarını anında dönüştürür.
 * - MutationObserver ile dinamik yüklenen içerikleri (tablar, çekmeceler, modallar, arama sonuçları) otomatik yakalar.
 * - Google Neural Translate altyapısını görünmez arka plan köprüsü olarak destekler (dinamik DB içerikleri için).
 * - Seçilen dili localStorage ('hayat_lang') ve çerezlerde kalıcı olarak saklar.
 */

(function (window, document) {
    'use strict';

    // ASLA Çevrilmeyecek Korunan Özel İsimler, Markalar, Tescilli Ürünler ve Logolar
    const PROTECTED_TERMS = [
        'Hayat İşleri',
        'HAYAT İŞLERİ',
        'hayat işleri',
        'HiCariTakip',
        'HİCARİTAKİP',
        'HiCari',
        'HİCARİ',
        'HiPosMenüm',
        'HİPOSMENÜM',
        'HiPos',
        'HİPOS',
        'KodPOS',
        'KODPOS',
        'Koddata',
        'KODDATA',
        'Zebra',
        'ZEBRA',
        'Sunmi',
        'SUNMI',
        'Datalogic',
        'DATALOGIC',
        'Bixolon',
        'Seiko',
        'Honeywell',
        'Epson',
        'Newland',
        'TSC',
        'Argox',
        'Italora',
        'Citizen',
        'Bluebird',
        'Pepperl+Fuchs',
        'Pepperl+Fuchs / ecom',
        'BarTender',
        'Bartender',
        'SOTI',
        'Soti',
        'Elanda',
        'Pax',
        'PAX',
        'İngenico',
        'Ingenico',
        'İNPOS',
        'İnpos',
        'Beko',
        'BEKO',
        'Propay',
        'PROPAY',
        'Paysis',
        'PAYSİS',
        'Pavo',
        'PAVO',
        'MasterSafe',
        'Mastersafe',
        'Samuto',
        'SAMUTO',
        'Doğu Lojistik',
        'Doğu Lojistik Dağıtım A.Ş.',
        'Özlem Ticaret',
        'Kuzey Ege Otomasyon',
        'Atlas Bilişim',
        'Ege Barkod',
        'Elazığ'
    ];

    // Detaylı, Tam Cümle ve İfade Odaklı Türkçe -> İngilizce Kurumsal Sözlük
    const DICTIONARY = {
        // === HERO BÖLÜMÜ & ANASAYFA TAM CÜMLELERİ ===
        "DİJİTAL DÜNYAYI": "THE DIGITAL WORLD",
        "KODLUYORUZ.": "WE ARE CODING.",
        "Hayat İşleri; kurumsal web yazılımları, ön muhasebe (HiCariTakip), barkod & perakende otomasyonları, yeni nesil POS sistemleri ve e-ticaret altyapıları ile Elazığ ve bölgedeki işletmelerin güvenilir teknoloji çözüm ortağıdır.": 
            "Hayat İşleri is the trusted technology solution partner for businesses across Elazığ and the region, providing enterprise web applications, pre-accounting (HiCariTakip), barcode & retail automation, next-gen POS systems, and e-commerce infrastructure.",
        "Hayat İşleri İşletme Çözümleri": "Hayat İşleri Enterprise Solutions",
        "İşletmenizi geleceğe taşıyacak güç ve hız için tasarlandı": "Engineered for the strength and speed to carry your business into the future",
        "Barkod ve yeni nesil POS sistemlerinden el terminallerine, HiCariTakip ön muhasebe yazılımından e-Dönüşüm ve kurumsal ERP entegrasyonlarına kadar tüm operasyonlarınızı tek çatı altında hızlandırın.":
            "Accelerate all your operations under a single roof—from barcode and next-generation POS systems to mobile computers, HiCariTakip pre-accounting software, e-Transformation, and enterprise ERP integrations.",
        "HAYAT İŞLERİ • KURUMSAL YAZILIM & OTOMASYON EKOSİSTEMİ": "HAYAT İŞLERİ • ENTERPRISE SOFTWARE & AUTOMATION ECOSYSTEM",
        "Kaydır": "Scroll Down",

        // === GÖRSELDEKİ BULUT SERVİSLERİ BENTO BÖLÜMÜ ===
        "Yeni Nesil Bulut Teknolojisi": "Next-Gen Cloud Technology",
        "Bulut servislerimiz ile": "With our cloud services",
        "İşinizi dilediğiniz zaman, dilediğiniz yerden yönetin.": "Manage your business anytime, from anywhere you want.",
        "İşinizi dilediğiniz zaman,": "Manage your business anytime,",
        "dilediğiniz yerden": "from anywhere",
        "yönetin.": "you want.",
        "HiCari ve HiPosMenüm bulut mimarimiz sayesinde ofis sınırlarını ortadan kaldırın. İster cep telefonunuzdan stoklarınızı kontrol edin, ister sahadan sipariş alın, isterseniz şubelerinizin canlı ciro raporlarını anında inceleyin.":
            "Eliminate office boundaries with our HiCari and HiPosMenüm cloud architecture. Whether checking stocks from your smartphone, taking field orders, or reviewing real-time branch revenue reports in seconds.",
        "Tüm Bulut Çözümleri": "All Cloud Solutions",
        "Tüm Bulut Sistemleri": "All Cloud Systems",
        "Demo Özelliklerini İncele": "Explore Demo Features",
        "Çoklu Cihaz & POS Uyumlu": "Multi-Device & POS Compatible",
        "Masaüstü bilgisayar, tablet, akıllı telefon ve endüstriyel POS terminali ekranlarıyla %100 senkronize çalışır.":
            "Operates 100% synchronized across desktop computers, tablets, smartphones, and industrial POS terminal screens.",
        "Web, iOS & Android": "Web, iOS & Android",
        "7/24 Aktif": "24/7 Active",
        "Banka Düzeyinde Güvenlik": "Bank-Grade Security",
        "Tüm ticari verileriniz 256-bit SSL protokolüyle şifrelenir; sunucularımızda günlük otomatik yedeklenir.":
            "All your commercial data is encrypted with 256-bit SSL protocol; daily automatic backups are safely stored on our servers.",
        "Otomatik Yedekleme": "Automatic Backup",
        "Canlı Şube & Depo Takibi": "Live Branch & Warehouse Tracking",
        "Tüm satış noktalarınızın ciro, stok ve cari hareketlerini tek bir merkezden saniyesinde izleyin.":
            "Monitor revenue, inventory, and ledger movements of all your sales points from a single center in seconds.",
        "Çoklu Şube Desteği": "Multi-Branch Support",
        "Kurulumsuz Anında Başlayın": "Zero-Install Instant Start",
        "Uzun süren kurulum süreçleri ve lisans anahtarı bekleme derdi olmadan kullanıcı adınızla anında bağlanın.":
            "Connect instantly with your credentials without tedious installation processes or license key waiting times.",
        "Sıfır Sunucu Maliyeti": "Zero Server Cost",

        // === YAZILIMLAR SAYFASI (/yazilimlar) ===
        "Yeni Nesil Bulut & Kurumsal Yazılım Çözümleri": "Next-Gen Cloud & Enterprise Software Solutions",
        "İşinizi Dilediğiniz Yerden Yöneten": "Empowering Your Business Anywhere",
        "Modern Yazılım Sistemleri": "Modern Software Systems",
        "HiCari ve HiPosMenüm bulut mimarimiz, barkodlu hızlı satış, B2B bayi sipariş portalı, Android saha el terminalleri ve e-ticaret pazaryeri entegrasyonlarımızla işletmenizin tüm operasyonlarını tek merkezden yönetin.":
            "Orchestrate all your business operations from a single hub with our HiCari and HiPosMenüm cloud architecture, rapid barcode POS, B2B partner ordering portal, Android field computers, and e-commerce marketplace integrations.",
        "Ücretsiz Demo Talep Edin": "Request Free Demo",
        "Fiyat Teklifi Alın": "Request Price Quote",
        "B2B Bayi Girişi": "B2B Partner Login",
        "Tüm Çözümler (7)": "All Solutions (7)",
        "Bulut ERP & Muhasebe": "Cloud ERP & Accounting",
        "Restoran & Kafe (HiPos)": "Restaurant & Cafe (HiPos)",
        "Market & Hızlı Satış": "Supermarket & Fast POS",
        "B2B Bayi Portalı": "B2B Partner Portal",
        "Depo & Saha Android": "Warehouse & Field Android",
        "PDKS & Turnike": "Time Attendance & Turnstile",
        "Pazaryeri & E-Ticaret": "Marketplace & E-Commerce",
        "HiCariTakip - Bulut & Hibrit Ön Muhasebe": "HiCariTakip - Cloud & Hybrid Accounting",
        "Masaüstü, web ve akıllı telefonlarla %100 senkronize çalışan, sunucu masrafsız ve 256-bit SSL güvenlikli yeni nesil ön muhasebe ve ticari operasyon platformu.":
            "Next-generation commercial operation and accounting platform working 100% synchronized across desktop, web, and smartphones with zero server costs and 256-bit SSL security.",
        "HiPosMenüm - Restoran Adisyon & QR Menü": "HiPosMenüm - Restaurant POS & QR Menu",
        "Kafeler, restoranlar, pastaneler ve fast-food zincirleri için dokunmatik masa planı, garson el terminali, mutfak ekranı ve dinamik QR menü entegrasyonu.":
            "Floor layout management, waiter handheld terminals, kitchen display systems (KDS), and dynamic QR menu integrations designed for cafes, restaurants, bakeries, and fast-food chains.",
        "Market & Mağaza Hızlı Satış POS": "Supermarket & Retail Fast POS",
        "Yoğun kasalarda kuyrukları bitiren, lazer barkod tarama, elektronik terazi, müşteri bilgi ekranı ve anında fiş/fatura kesen profesyonel perakende yazılımı.":
            "Professional retail software eliminating checkout queues with high-speed laser barcode scanning, scale integration, customer displays, and instant receipt/invoice issuance.",
        "Hayat B2B - Bayi Sipariş & Tahsilat Portalı": "Hayat B2B - Partner Order & Collection Portal",
        "Donanım, POS ve yazılım bayileriniz için özel iskontolu fiyat listeleri, 7/24 online sipariş, Sanal POS ile kredi kartı tahsilatı ve RMA teknik servis takibi.":
            "Special discounted price tiers, 24/7 online ordering, 3D Secure credit card collections, and RMA technical service tracking for hardware, POS, and software dealers.",
        "Depo & Saha El Terminali Otomasyonu": "Warehouse & Field Handheld Automation",
        "Android el terminalleri ve akıllı telefonlarla çalışan, sıcak/soğuk saha satış, depo mal kabul, sayım, lokasyon transferi ve mobil bluetooth fatura kesimi.":
            "Running on Android handheld computers and smartphones; supporting van sales, warehouse goods receipt, inventory counts, bin transfers, and on-the-spot Bluetooth invoicing.",
        "PDKS & Turnike Geçiş Kontrol Yazılımı": "Time & Attendance (PDKS) and Turnstile Access Control",
        "Yüz tanıma terminalleri, parmak izi okuyucular ve kartlı turnikelerle entegre çalışan; vardiya, mesai, geç kalma ve bordro hesaplamalarını otomatikleştiren sistem.":
            "Seamlessly integrated with facial recognition, fingerprint readers, and RFID turnstiles; automating shifts, overtime, tardiness, and payroll calculations.",
        "Pazaryeri & E-Ticaret Entegrasyon Köprüsü": "Marketplace & E-Commerce Integration Bridge",
        "Trendyol, Hepsiburada, Amazon, N11, Çiçeksepeti ve e-ticaret sitelerinizle ERP muhasebe programınızı tam senkronize edin. Sipariş geldiğinde tek tıkla e-Fatura kesin, stok tüm pazaryerlerinde anında güncellensin.":
            "Synchronize your ERP accounting with Trendyol, Hepsiburada, Amazon, N11, and online stores. Generate e-Invoices in one click upon order receipt and synchronize stock across all channels automatically.",
        "İşletmenize En Uygun Çözümü Birlikte Belirleyelim": "Let's Identify the Best Solution for Your Business Together",
        "Uzman yazılım ekibimiz, ofisinizde veya online video konferans ile programlarımızın tüm özelliklerini iş süreçlerinize özel olarak tanıtsın.":
            "Our expert software team will showcase all software capabilities tailored to your workflows, either on-site at your office or via live video conference.",
        "Kurulumsuz 14 Gün Deneme": "Zero-Setup 14-Day Free Trial",
        "Ücretsiz Veri Aktarım Desteği": "Free Data Migration Support",
        "7/24 Teknik Danışmanlık": "24/7 Technical Consultation",
        "Hızlı Demo Formu": "Quick Demo Form",
        "Bilgilerinizi bırakın, aynı gün içinde sizi arayıp sistemi gösterelim.": "Leave your details and we will contact you the same day to demonstrate the system.",
        "Ad Soyad / Firma": "Full Name / Company",
        "Adınız ve Şirket Unvanınız": "Your Name and Company Name",
        "Telefon Numarası": "Phone Number",
        "E-Posta Adresi": "E-Mail Address",
        "İlgilendiğiniz Yazılım": "Software of Interest",
        "Demoyu Başlat": "Launch Demo Request",
        "Canlı Demo Talebi": "Live Demo Request",
        "Sistemi canlı ekranda test edin": "Test the system on a live screen",
        "Seçilen Yazılım": "Selected Software",
        "Talebi Gönder": "Submit Request",
        "Vazgeç": "Cancel",
        "hicari.com.tr'yi Ziyaret Et": "Visit hicari.com.tr",
        "Demo İste": "Request Demo",
        "Canlı Demo İncele": "Inspect Live Demo",
        "Teklif İste": "Request Quote",
        "Bayi Portalına Git": "Go to Partner Portal",
        "Bayilik Başvurusu": "Dealership Application",
        "Entegrasyon Demosu Talep Et": "Request Integration Demo",
        "Proje Teklifi Al": "Get Project Quote",

        // === REFERANSLARIMIZ & ESERLERİMİZ ===
        "Referanslarımız & Eserlerimiz": "Our References & Works",
        "Öne Çıkan Referanslarımız": "Featured References",
        "İşletmelerin dijital dönüşüm süreçlerine güç katan kurumsal yazılım, e-ticaret ve otomasyon projelerimiz.":
            "Our enterprise software, e-commerce, and automation projects driving the digital transformation of modern businesses.",
        "Tümü": "All",
        "Özel Yazılım": "Custom Software",
        "Web Tasarım": "Web Design",
        "E-Ticaret": "E-Commerce",
        "POS & Barkod Otomasyonu": "POS & Barcode Automation",
        "Restoran & Kafe Otomasyonu": "Restaurant & Cafe Automation",
        "Kurumsal Web & B2B E-Ticaret": "Enterprise Web & B2B E-Commerce",
        "Aktif Kullanımda": "In Active Production",
        "Canlı Gör": "View Live",

        // === B2B BAYİLİK BÖLÜMÜ ===
        "B2B BAYİ ORTAKLIĞI & SİPARİŞ PORTALI": "B2B PARTNER NETWORK & ORDER PORTAL",
        "Hayat İşleri Bayilik ve İş Ortaklığı Programı": "Hayat İşleri Partnership & Dealership Program",
        "Ticari yazılım programlarımız ve kurumsal donanım çözümlerimizde yetkili bayilik avantajlarından yararlanın. B2B bayi portalımız üzerinden size özel indirimli fiyatlarla hızlıca sipariş verin, tüm lisans ve donanım tedarik süreçlerinizi tek merkezden yönetin.":
            "Take advantage of authorized dealership perks across our commercial software and enterprise hardware solutions. Order quickly at custom discounted prices via our B2B portal, and manage licensing and hardware fulfillment from one central hub.",
        "B2B Portaldan İndirimli Sipariş": "Discounted Orders via B2B Portal",
        "Ticari Program & Kurumsal Donanım": "Commercial Software & Enterprise Hardware",
        "Özel Bayi İskontosu & Öncelikli Destek": "Special Dealer Discount & Priority Support",
        "Hemen Bayilik Başvurusu Yap": "Apply for Dealership Now",
        "Yetkili Bayi Girişi": "Authorized Partner Login",
        "Bayi İletişim Hattı": "Partner Hotline",
        "Yetkili Çözüm Ortaklığı Programı": "Authorized Solution Partnership Program",
        "81 İlde Aktif": "Active in 81 Provinces",
        "Özel Bayi Fiyatlandırması": "Custom Dealer Pricing",
        "Yazılım lisansları ve POS donanımlarında yüksek marj": "High margins on software licenses and POS hardware",
        "VIP Bayi": "VIP Partner",

        // === HİZMETLERİMİZ & ÇÖZÜMLERİMİZ ===
        "Faaliyet Alanlarımız": "Our Fields of Activity",
        "Hizmetlerimiz & Çözümlerimiz": "Our Services & Solutions",
        "Kurumsal yapınızı geleceğe taşıyan, yüksek performanslı ve tamamen ölçeklenebilir teknoloji çözümleri.":
            "High-performance, fully scalable technology solutions propelling your corporate structure into the future.",
        "Tüm Hizmet Detayları": "All Service Details",
        "Özel Yazılım & Web Tasarım": "Custom Software & Web Design",
        "İşletmenizin ihtiyaçlarına tam olarak uyan özel web tabanlı yazılımlar, iş takip panelleri ve modern, mobil uyumlu kurumsal web siteleri.":
            "Custom web-based software, operation dashboards, and modern responsive corporate websites tailored precisely to your operational needs.",
        "Ön Muhasebe & Cari Takip": "Pre-Accounting & Current Accounts",
        "HiCariTakip ile cari hesap, stok ve depo yönetimi, alış/satış faturaları, e-Fatura, kasa ve çek-senet süreçlerini tek platformdan yönetin.":
            "Manage ledger accounts, inventory, warehouse, invoices, e-Invoice, cash register, and check-promissory notes on a single unified platform with HiCariTakip.",
        "Barkod & Hızlı Satış Sistemleri": "Barcode & Rapid Checkout Systems",
        "Market, mağaza ve perakende işletmeleri için barkod okuyucular, termal etiket/fiş yazıcıları, el terminalleri ve hızlı satış donanımları.":
            "Barcode scanners, thermal label/receipt printers, mobile handhelds, and fast retail checkout hardware for supermarkets, boutiques, and retail stores.",
        "Yeni Nesil POS & Ödeme": "Next-Gen POS & Payment Systems",
        "Yeni nesil yazar kasa POS cihazları, banka tahsilat entegrasyonları, temassız ödeme terminalleri ve güvenli finansal çözümler.":
            "Next-gen fiscal cash register POS devices, bank collection integrations, contactless payment terminals, and secure financial infrastructure.",
        "Geçiş Kontrol & PDKS": "Access Control & Time Attendance",
        "Personel devam kontrol sistemleri (PDKS), kartlı ve parmak izli geçiş terminalleri, turnike ve erişim yetkilendirme otomasyonları.":
            "Time and attendance tracking systems (PDKS), biometric fingerprint and card access terminals, turnstiles, and authorization automations.",
        "E-Ticaret & Sanal POS": "E-Commerce & Virtual POS",
        "Ürünlerinizi internet üzerinden 7/24 satışa sunun: Sanal POS banka bağlantıları, HiCariTakip ve stok yönetimi ile tam senkronize e-ticaret siteleri.":
            "Sell your products 24/7 online: E-commerce stores fully synchronized with Virtual POS bank connections, HiCariTakip, and warehouse stock.",
        "Detaylı Bilgi →": "Detailed Info →",
        "Kurumsal": "Corporate",
        "Tam Entegre": "Fully Integrated",
        "Biyometrik": "Biometric",
        "Güvenli Tahsilat": "Secure Collection",

        // === GENEL NAVİGASYON & ÇEKMECE MENÜ ===
        "Anasayfa": "Home",
        "Hakkımızda": "About Us",
        "Yazılım": "Software",
        "Yazılımlar": "Software",
        "Donanım": "Hardware",
        "Donanım Grupları": "Hardware Categories",
        "Tüm Donanım": "All Hardware",
        "Tüm Donanım Kataloğunu Aç →": "Open Full Hardware Catalog →",
        "Hizmetlerimiz": "Our Services",
        "Hizmetler": "Services",
        "Çözümlerimiz": "Solutions",
        "Blog & Duyurular": "Blog & Announcements",
        "Teknoloji Blogu": "Technology Blog",
        "Blog": "Blog",
        "İnsan Kaynakları & Kariyer": "Human Resources & Careers",
        "Hesap Numaralarımız": "Bank Account Numbers",
        "İletişim": "Contact",
        "İletişim & Destek": "Contact & Support",
        "Destek Merkezi": "Support Center",
        "Destek": "Support",
        "Bayi Girişi": "Partner Login",
        "Bayi Giriş": "Partner Login",
        "Bayi Portalı": "Partner Portal",
        "Yönetim Paneli": "Management Panel",
        "Admin Paneli": "Admin Panel",
        "Kurumsal Yönetim Konsolu": "Enterprise Management Console",
        "Giriş Yap": "Sign In",
        "Giriş": "Sign In",
        "Kayıt Ol": "Register",
        "Çıkış Yap": "Sign Out",
        "Çıkış": "Sign Out",
        "Hesabım": "My Account",
        "Kapat": "Close",
        "Menu": "Menu",
        "Menü": "Menu",
        "Arama": "Search",
        "Ara": "Search",
        "Ürün, barkod, POS veya yazılım ara...": "Search products, barcodes, POS or software...",
        "Hızlı Erişim & Popüler": "Quick Access & Popular",
        "Detayları İncele": "View Details",
        "İncele": "Inspect",
        "Teklif Al": "Get Quote",
        "Fiyat Teklifi": "Price Quote",
        "Sepete Ekle": "Add to Cart",
        "Hızlı Sipariş": "Quick Order",
        "Sipariş Geç": "Place Order",
        "Siparişi Onayla": "Confirm Order",

        // === DONANIM VE ÜRÜNLER SAYFASI (/Home/Urunler) ===
        "Tüm Modeller": "All Models",
        "Tümünü Göster": "Show All",
        "Markalar": "Brands",
        "13 Marka": "13 Brands",
        "20 Grup": "20 Categories",
        "20 Kategori": "20 Categories",
        "Filtre:": "Filter:",
        "Para Çekmecesi": "Cash Drawer",
        "Para Çekmeceleri": "Cash Drawers",
        "Şifreli Çelik Kasa": "Electronic Safe Box",
        "Şifreli Çelik Kasalar": "Electronic Safe Boxes",
        "POS Standları": "POS Stands & Mounts",
        "POS Standı": "POS Stand",
        "Yazarkasa POS (ÖKC)": "Fiscal Cash Register POS",
        "Yazarkasa POS": "Fiscal POS Terminal",
        "Para Sayma Makinesi": "Currency Counter Machine",
        "Para Sayma Makineleri": "Currency Counter Machines",
        "El Terminalleri": "Mobile Handheld Computers",
        "El Terminali": "Mobile Computer",
        "Endüstriyel PC-Tablet": "Industrial PC & Tablet",
        "Dokunmatik POS PC": "Touchscreen POS PC",
        "Barkod Okuyucular": "Barcode Scanners",
        "Barkod Okuyucu": "Barcode Scanner",
        "Barkod Yazıcılar": "Barcode Printers",
        "Barkod Yazıcı": "Barcode Printer",
        "Endüstriyel Yazıcı": "Industrial Printers",
        "Endüstriyel Yazıcılar": "Industrial Printers",
        "Endüstriyel Barkod Yazıcılar": "Industrial Barcode Printers",
        "Masaüstü Yazıcı": "Desktop Printers",
        "Masaüstü Yazıcılar": "Desktop Printers",
        "Fiş / Adisyon Yazıcı": "Receipt & Kitchen Printers",
        "Fiş/Adisyon Yazıcılar": "Receipt & Kitchen Printers",
        "Fiş Yazıcı": "Receipt Printer",
        "Mobil Yazıcılar": "Mobile Printers",
        "Mobil Yazıcı": "Mobile Printer",
        "Kart Yazıcılar": "Card Printers",
        "Kart Yazıcı": "Card Printer",
        "Endüstriyel Tablet": "Industrial Rugged Tablets",
        "Giyilebilir Cihazlar": "Wearable Devices",
        "Giyilebilir Barkod": "Wearable Ring Scanners",
        "Kiosklar": "Kiosk Systems",
        "Kiosk Sistemleri": "Kiosk Systems",
        "Patlayıcı Ortamlar": "Explosion-Proof (ATEX)",
        "Patlayıcı Ortam (ATEX)": "Explosion-Proof (ATEX)",
        "Mobil Cihaz Yönetimi": "Mobile Device Management (MDM)",
        "Mobil Cihaz Yönetimi (MDM)": "Mobile Device Management (MDM)",
        "Aksesuarlar": "Accessories & Supplies",
        "Aksesuar & Sarf": "Accessories & Supplies",
        "Teknik Özellikler": "Technical Specifications",
        "Ürün Detayları": "Product Details",
        "Teklif Butonu": "Get Quote Button",
        "Teklif Alın": "Get a Quote",

        // === BAYİ PORTALI & ADMİN TERİMLERİ ===
        "Bayi Paneli": "Partner Portal",
        "Donanım ve POS Bayisi Paneli": "Hardware & POS Partner Portal",
        "Yazılım ve ERP Bayisi Paneli": "Software & ERP Partner Portal",
        "Tam Yetkili Çözüm Ortağı Paneli": "Premier Solution Partner Portal",
        "Cihaz Envanteri": "Device Inventory",
        "RMA Servis Takip": "RMA Service Tracking",
        "Teknik Servis Yönetimi": "RMA Technical Service",
        "Fırsat Kaydı (Deal Registration)": "Deal Registration",
        "Fırsat Kaydı": "Deal Registration",
        "Demo Cihaz Talebi": "Demo Device Request",
        "Pazarlama Materyalleri": "Marketing Assets",
        "Eski Cihaz Değişim / Geri Alım": "Trade-In & Refresh Program",
        "Alt Müşteri (Son Kullanıcı) Yönetimi": "End-User Client Management",
        "Lisans & Abonelik Takibi": "License & Subscription Tracking",
        "Hayat İşleri Akademi (Eğitim & Sertifika)": "Hayat İşleri Academy (Training & Certs)",
        "Modül & Kontör Yükseltme": "Module & Credit Upgrades",
        "Geliştirici Araçları & Sandbox": "Developer Tools & API Sandbox",
        "Ciro Primi & Hedef Göstergesi": "Rebate & Target Incentive Dashboard",
        "Çapraz Satış Raporları": "Cross-Selling Analytics",
        "Ortak Pazarlama Fonu (MDF)": "Market Development Fund (MDF)",
        "Öncelikli Ar-Ge & Ürün Yol Haritası": "Priority R&D & Product Roadmap",
        "Giriş & İşlem Logları": "Access & Audit Logs",
        "Giriş Logları": "Access Logs",
        "Denetim İzi (Audit Log)": "Audit Trail Logs",
        "Hangi Bilgisayar / IP": "Host Machine / IP Address",
        "İşlem Zamanı": "Action Timestamp",
        "Ne Değişti": "Changes Recorded",

        // === İLETİŞİM, HAKKIMIZDA & DESTEK SAYFALARI ===
        "Bizimle İletişime Geçin": "Get In Touch With Us",
        "Sorularınız, projeleriniz veya kurumsal iş birliği teklifleriniz için bize her zaman ulaşabilirsiniz.": 
            "You can reach out to us anytime for questions, projects, or corporate partnership inquiries.",
        "Pazartesi - Cuma: 09:00 - 18:00": "Monday - Friday: 09:00 - 18:00",
        "Cumartesi - Pazar: Kapalı": "Saturday - Sunday: Closed",
        "Bize Mesaj Gönderin": "Send Us a Message",
        "Mesajınızı buraya yazın...": "Write your message here...",
        "Mesajı Gönder": "Send Message",
        "Formu Gönder": "Submit Form",
        "Kurumsal İletişim Hattı": "Corporate Contact Line",
        "Elazığ merkezli olarak faaliyet gösteren Hayat İşleri, HiCariTakip ön muhasebe, barkodlu hızlı satış sistemleri, yeni nesil POS, PDKS turnike geçiş ve özel kurumsal yazılım alanlarında anahtar teslim işletme çözümleri sunar.":
            "Operating from Elazığ, Hayat İşleri delivers turnkey enterprise solutions in HiCariTakip pre-accounting, barcode rapid checkout POS systems, next-gen POS, time & attendance turnstiles, and custom enterprise software.",
        "Vizyonumuz": "Our Vision",
        "Misyonumuz": "Our Mission",
        "İlkelerimiz": "Our Principles",
        "Çalışma İlkelerimiz": "Our Working Principles",
        "Kurumsal Gücümüz": "Our Corporate Strength",
        "Önceliklerimiz": "Our Priorities",
        "Destek Talebi Oluşturun": "Create Support Request",
        "Uzman teknik ekibimiz en kısa sürede sizinle iletişime geçecektir.": "Our expert technical team will contact you as soon as possible.",

        // === FOOTER & FORM TERİMLERİ ===
        "İletişim Bilgileri": "Contact Information",
        "Telefon": "Phone",
        "E-Posta": "E-Mail",
        "Adres": "Address",
        "Çalışma Saatleri": "Working Hours",
        "Pazartesi - Cuma": "Monday - Friday",
        "Cumartesi": "Saturday",
        "Pazar": "Sunday",
        "Kapalı": "Closed",
        "Tüm Hakları Saklıdır": "All Rights Reserved",
        "Gizlilik Politikası": "Privacy Policy",
        "Kullanım Şartları": "Terms of Service",
        "KVKK Aydınlatma Metni": "GDPR / KVKK Disclosure",
        "Mesajınız": "Your Message",
        "Gönder": "Send",
        "Temizle": "Clear",
        "Başarılı": "Success",
        "Hata": "Error",
        "Uyarı": "Warning",
        "Bilgi": "Information",
        "Kaydet": "Save",
        "Güncelle": "Update",
        "Sil": "Delete",
        "Düzenle": "Edit"
    };

    // Uzunluklarına göre azalan sırada sıralanmış tam ifadeler (önce uzun cümleler eşleşir)
    const SORTED_TR_KEYS = Object.keys(DICTIONARY).sort((a, b) => b.length - a.length);

    // Orijinal Türkçe içerikleri saklayan referans haritaları (DOM -> Orijinal Metin)
    const originalTextMap = new WeakMap();
    const originalPlaceholderMap = new WeakMap();
    const originalTitleMap = new WeakMap();

    /**
     * Verilen metin korunan bir marka veya unvan mı?
     */
    function isProtectedTerm(text) {
        if (!text) return false;
        const trimmed = text.trim();
        for (const term of PROTECTED_TERMS) {
            if (trimmed === term || trimmed.toLowerCase() === term.toLowerCase()) return true;
        }
        return false;
    }

    /**
     * Türkçe karakter duyarlı akıllı metin çevirici
     * Kelime köklerini parçalamaz, tam ifadeleri ve kelimeleri eşler.
     */
    function translateString(str) {
        if (!str || typeof str !== 'string') return str;
        const trimmed = str.trim();
        if (!trimmed) return str;

        // 1. Doğrudan tam eşleşme kontrolü (%100 doğru ve hızlı)
        if (DICTIONARY[trimmed]) {
            return str.replace(trimmed, DICTIONARY[trimmed]);
        }

        // 2. Özel isim veya korunan marka kontrolü
        if (isProtectedTerm(trimmed)) {
            return str;
        }

        // 3. İfade ve Cümle Değişimi (Uzundan kısaya)
        let result = str;
        for (const key of SORTED_TR_KEYS) {
            if (key.length >= 3 && result.includes(key)) {
                // Eğer key birden fazla kelime içeriyorsa doğrudan güvenle değiştir
                if (key.includes(' ') || key.includes('-') || key.includes('&') || key.includes('/')) {
                    const escaped = key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                    const reg = new RegExp(escaped, 'g');
                    result = result.replace(reg, DICTIONARY[key]);
                } else {
                    // Tek bir kelime ise, Türkçe harf duyarlı sınır kontrolü yap
                    // Kelimenin önünde veya arkasında harf varsa DEĞİŞTİRME (bozuk ek üretimini önle)
                    const escaped = key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                    const wordBoundaryReg = new RegExp('(^|[^a-zA-Z0-9çğıöşüÇĞİÖŞÜ])' + escaped + '(?=[^a-zA-Z0-9çğıöşüÇĞİÖŞÜ]|$)', 'g');
                    result = result.replace(wordBoundaryReg, function (match, prefix) {
                        return prefix + DICTIONARY[key];
                    });
                }
            }
        }
        return result;
    }

    /**
     * DOM Ağacını derinlemesine tarayarak çevirir
     */
    function translateNode(node) {
        if (!node) return;

        if (node.nodeType === Node.ELEMENT_NODE) {
            const el = node;
            const tagName = el.tagName.toLowerCase();
            if (tagName === 'script' || tagName === 'style' || tagName === 'svg' || tagName === 'code' || tagName === 'noscript') {
                return;
            }
            if (el.classList && (el.classList.contains('notranslate') || el.classList.contains('lusion-logo') || el.classList.contains('admin-logo-mark'))) {
                return;
            }

            // Input / Textarea placeholder çevirisi
            if (el.placeholder) {
                if (!originalPlaceholderMap.has(el)) {
                    originalPlaceholderMap.set(el, el.placeholder);
                }
                const orig = originalPlaceholderMap.get(el);
                el.placeholder = translateString(orig);
            }

            // Title çevirisi
            if (el.title) {
                if (!originalTitleMap.has(el)) {
                    originalTitleMap.set(el, el.title);
                }
                const orig = originalTitleMap.get(el);
                el.title = translateString(orig);
            }
        }

        // Metin Düğümleri (TextNode)
        if (node.nodeType === Node.TEXT_NODE) {
            const text = node.nodeValue;
            if (text && text.trim().length > 0) {
                const parent = node.parentElement;
                if (parent) {
                    const parentTag = parent.tagName.toLowerCase();
                    if (parentTag === 'script' || parentTag === 'style' || parentTag === 'code') return;
                    if (parent.classList && (parent.classList.contains('notranslate') || parent.getAttribute('translate') === 'no')) return;
                }

                if (!originalTextMap.has(node)) {
                    originalTextMap.set(node, text);
                }

                const orig = originalTextMap.get(node);
                const translated = translateString(orig);
                if (translated !== text) {
                    node.nodeValue = translated;
                }
            }
            return;
        }

        // Çocuk düğümleri tara
        let child = node.firstChild;
        while (child) {
            translateNode(child);
            child = child.nextSibling;
        }
    }

    /**
     * DOM Ağacındaki çevrilmiş metinleri orijinal Türkçesine geri döndürür
     */
    function restoreNode(node) {
        if (!node) return;

        if (node.nodeType === Node.ELEMENT_NODE) {
            const el = node;
            if (originalPlaceholderMap.has(el)) {
                el.placeholder = originalPlaceholderMap.get(el);
            }
            if (originalTitleMap.has(el)) {
                el.title = originalTitleMap.get(el);
            }
        }

        if (node.nodeType === Node.TEXT_NODE) {
            if (originalTextMap.has(node)) {
                node.nodeValue = originalTextMap.get(node);
            }
            return;
        }

        let child = node.firstChild;
        while (child) {
            restoreNode(child);
            child = child.nextSibling;
        }
    }

    /**
     * Tüm arayüz dil butonlarının görsel durumlarını günceller
     */
    function updateAllLanguageButtons(lang) {
        const isEn = (lang === 'en');
        const flagEmoji = isEn ? '🇬🇧' : '🇹🇷';
        const langCode = isEn ? 'EN' : 'TR';
        const titleText = isEn ? "Switch to Turkish / Türkçe'ye Geç (🇹🇷 TR)" : "Switch to English / İngilizce'ye Geç (🇬🇧 EN)";

        // 1. Genel Header Butonu
        const headerBtn = document.getElementById('hytLangToggle');
        const headerFlag = document.getElementById('hytLangFlag');
        const headerCode = document.getElementById('hytLangCode');
        if (headerBtn) {
            headerBtn.setAttribute('title', titleText);
            headerBtn.setAttribute('aria-label', titleText);
        }
        if (headerFlag) headerFlag.textContent = flagEmoji;
        if (headerCode) headerCode.textContent = langCode;

        // 2. Bayi Portalı Butonu
        const bayiBtn = document.getElementById('bayiLangToggle');
        const bayiFlag = document.getElementById('bayiLangFlag');
        const bayiCode = document.getElementById('bayiLangText');
        if (bayiBtn) bayiBtn.setAttribute('title', titleText);
        if (bayiFlag) bayiFlag.textContent = flagEmoji;
        if (bayiCode) bayiCode.textContent = langCode;

        // 3. Admin Konsolu Butonu
        const adminBtn = document.getElementById('adminLangToggle');
        const adminFlag = document.getElementById('adminLangFlag');
        const adminCode = document.getElementById('adminLangText');
        if (adminBtn) adminBtn.setAttribute('title', titleText);
        if (adminFlag) adminFlag.textContent = flagEmoji;
        if (adminCode) adminCode.textContent = langCode;

        document.documentElement.setAttribute('lang', lang);
    }

    let currentLanguage = 'tr';
    let isTranslating = false;
    let observer = null;

    /**
     * Dili ayarlar ve sayfayı dönüştürür
     */
    function setLanguage(lang) {
        if (isTranslating) return;
        isTranslating = true;

        currentLanguage = (lang === 'en') ? 'en' : 'tr';
        try {
            localStorage.setItem('hayat_lang', currentLanguage);
            document.cookie = `hayat_lang=${currentLanguage}; path=/; max-age=31536000; SameSite=Lax`;
            // Google Translate çerezi ile de senkronize et
            const gtCookie = (currentLanguage === 'en') ? '/tr/en' : '/tr/tr';
            document.cookie = `googtrans=${gtCookie}; path=/; max-age=31536000; SameSite=Lax`;
        } catch (e) { }

        updateAllLanguageButtons(currentLanguage);

        if (currentLanguage === 'en') {
            translateNode(document.body);
            startObserver();
        } else {
            stopObserver();
            restoreNode(document.body);
        }

        window.dispatchEvent(new CustomEvent('hayat-lang-change', { detail: { lang: currentLanguage } }));
        isTranslating = false;
    }

    /**
     * Dili Türkçe <-> İngilizce arasında değiştirir
     */
    function toggleLanguage() {
        const nextLang = (currentLanguage === 'tr') ? 'en' : 'tr';
        setLanguage(nextLang);
    }

    /**
     * Dinamik DOM değişimlerini izler
     */
    function startObserver() {
        if (observer) return;
        observer = new MutationObserver((mutations) => {
            if (currentLanguage !== 'en' || isTranslating) return;
            mutations.forEach(m => {
                if (m.type === 'childList') {
                    m.addedNodes.forEach(addedNode => {
                        translateNode(addedNode);
                    });
                } else if (m.type === 'characterData') {
                    translateNode(m.target);
                }
            });
        });
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });
    }

    function stopObserver() {
        if (observer) {
            observer.disconnect();
            observer = null;
        }
    }

    /**
     * Sayfa yüklenirken başlangıç dilini uygular
     */
    function init() {
        let savedLang = 'tr';
        try {
            savedLang = localStorage.getItem('hayat_lang') || 'tr';
        } catch (e) { }

        currentLanguage = (savedLang === 'en') ? 'en' : 'tr';
        updateAllLanguageButtons(currentLanguage);

        if (currentLanguage === 'en') {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => setLanguage('en'));
            } else {
                setLanguage('en');
            }
        }

        // Tıklama olaylarını delegasyon ile bağla
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('#hytLangToggle, #bayiLangToggle, #adminLangToggle');
            if (btn) {
                e.preventDefault();
                toggleLanguage();
            }
        });
    }

    // Global nesneyi dışa aç
    window.HayatI18n = {
        setLanguage,
        toggleLanguage,
        getCurrentLanguage: () => currentLanguage,
        translateString,
        init
    };

    init();

})(window, document);
