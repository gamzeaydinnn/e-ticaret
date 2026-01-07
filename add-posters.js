// Posterler eklemek için test script
const http = require('http');

const posters = [
    {
        title: "İlk Alışveriş İndirimi",
        imageUrl: "/images/ilk-alisveris-indirim-banner.png",
        linkUrl: "/kampanyalar/ilk-alisveris",
        type: "slider",
        displayOrder: 1,
        isActive: true
    },
    {
        title: "Taze ve Doğal İndirim Reyonu",
        imageUrl: "/images/taze-dogal-indirim-banner.png",
        linkUrl: "/kategori/meyve-ve-sebze",
        type: "slider",
        displayOrder: 2,
        isActive: true
    },
    {
        title: "Meyve Reyonumuz",
        imageUrl: "/images/meyve-reyonu-banner.png",
        linkUrl: "/kategori/meyve-ve-sebze",
        type: "slider",
        displayOrder: 3,
        isActive: true
    },
    {
        title: "Özel Fiyat Köy Sütü",
        imageUrl: "/images/ozel-fiyat-koy-sutu.png",
        linkUrl: "/urun/koy-sutu",
        type: "promo",
        displayOrder: 1,
        isActive: true
    },
    {
        title: "Temizlik Malzemeleri",
        imageUrl: "/images/temizlik-malzemeleri.png",
        linkUrl: "/kategori/temizlik",
        type: "promo",
        displayOrder: 2,
        isActive: true
    },
    {
        title: "Taze Günlük Lezzetli",
        imageUrl: "/images/taze-gunluk-lezzetli.png",
        linkUrl: "/kategori/sut-ve-sut-urunleri",
        type: "promo",
        displayOrder: 3,
        isActive: true
    },
    {
        title: "Gölköy Gurme Et",
        imageUrl: "/images/golkoy-banner-2.png",
        linkUrl: "/kategori/et-ve-et-urunleri",
        type: "promo",
        displayOrder: 4,
        isActive: true
    }
];

function addPoster(poster) {
    return new Promise((resolve, reject) => {
        const data = JSON.stringify(poster);
        
        const options = {
            hostname: 'localhost',
            port: 5153,
            path: '/api/banners',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Content-Length': Buffer.byteLength(data),
                'X-Requested-With': 'XMLHttpRequest'
            }
        };

        const req = http.request(options, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            
            res.on('end', () => {
                console.log(`[${res.statusCode}] ${poster.title}`);
                if (res.statusCode >= 400) {
                    console.error(`  Error: ${responseData}`);
                }
                resolve();
            });
        });

        req.on('error', (error) => {
            console.error(`ERROR (${poster.title}): ${error.message}`);
            reject(error);
        });

        req.write(data);
        req.end();
    });
}

async function main() {
    console.log('=== Posterler Ekleniyor ===\n');
    
    for (const poster of posters) {
        await addPoster(poster);
    }
    
    console.log('\n=== İşlem Tamamlandı ===');
}

main();
