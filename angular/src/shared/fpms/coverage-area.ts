export interface CoverageCity {
    name: string;
    postalCode: string;
    barangays?: string[];
}

export interface CoverageProvince {
    name: string;
    cities: CoverageCity[];
}

export const COVERAGE_AREA: CoverageProvince[] = [
    { name: 'South Cotabato', cities: [{ name: 'General Santos City', postalCode: '9500',
            barangays: ['Apopong', 'Baluan', 'Batomelong', 'Buayan', 'Bula', 'Calumpang', 'City Heights', 'Conel', 'Dadiangas East', 'Dadiangas North', 'Dadiangas South', 'Dadiangas West', 'Fatima', 'Katangawan', 'Labangal', 'Lagao (1st & 3rd)', 'Ligaya', 'Mabuhay', 'Olympog', 'San Isidro (Lagao 2nd)', 'San Jose', 'Siguel (Bawing)', 'Sinawal', 'Tambler', 'Tinagacan', 'Upper Labay'] }] },
    {
        name: 'Sarangani',
        cities: [
            { name: 'Alabel', postalCode: '9501',
            barangays: ['Alegria', 'Bagacay', 'Baluntay', 'Datal Anggas', 'Domolok', 'Kawas', 'Ladol', 'Maribulan', 'New Canaan', 'Pag-asa', 'Paraiso', 'Poblacion', 'Spring', 'Tokawal'] },
            { name: 'Glan', postalCode: '9517',
            barangays: ['Baliton', 'Batotuling', 'Batulaki', 'Big Margus', 'Burias', 'Cablalan', 'Calabanit', 'Calpidong', 'Congan', 'Cross', 'Datalbukay', 'E. Alegado', 'Glan Padidu', 'Gumasa', 'Ilaya', 'Kaltuad', 'Kapatan', 'Lago', 'Laguimit', 'Mudan', 'New Aklan', 'Pangyan', 'Poblacion', 'Rio Del Pilar', 'San Jose', 'San Vicente', 'Small Margus', 'Sufatubo', 'Taluya', 'Tango', 'Tapon'] },
            { name: 'Kiamba', postalCode: '9514',
            barangays: ['Badtasan', 'Datu Dani', 'Gasi', 'Kapate', 'Katubao', 'Kayupo', 'Kling (Lumit)', 'Lagundi', 'Lebe', 'Lomuyon', 'Luma', 'Maligang', 'Nalus', 'Poblacion', 'Salakit', 'Suli', 'Tablao', 'Tamadang', 'Tambilil'] },
            { name: 'Maasim', postalCode: '9502',
            barangays: ['Amsipit', 'Bales', 'Colon', 'Daliao', 'Kabatiol', 'Kablacan', 'Kamanga', 'Kanalo', 'Lumasal', 'Lumatil', 'Malbang', 'Nomoh', 'Pananag', 'Poblacion', 'Seven Hills', 'Tinoto'] },
            { name: 'Maitum', postalCode: '9515',
            barangays: ['Batian', 'Kalaneg', 'Kalaong', 'Kiambing', 'Kiayap', 'Mabay', 'Maguling', 'Malalag', 'Mindupok', 'New La Union', 'Old Poblacion', 'Pangi', 'Pinol', 'Sison', 'Ticulab', 'Tuanadatu', 'Upo', 'Wali', 'Zion'] },
            { name: 'Malapatan', postalCode: '9516',
            barangays: ['Daan Suyan', 'Kihan', 'Kinam', 'Libi', 'Lun Masla', 'Lun Padidu', 'Patag', 'Poblacion', 'Sapu Masla', 'Sapu Padidu', 'Tuyan', 'Upper Suyan'] },
            { name: 'Malungon', postalCode: '9503' },
        ],
    },
];

export function coverageProvinces(current?: string): string[] {
    const names = COVERAGE_AREA.map((p) => p.name);
    return current && !names.includes(current) ? [...names, current] : names;
}

export function coverageCities(province: string, current?: string): string[] {
    const names = COVERAGE_AREA.find((p) => p.name === province)?.cities.map((c) => c.name) ?? [];
    return current && !names.includes(current) ? [...names, current] : names;
}

export function applyCoverageProvince(model: { province?: string; cityMunicipality?: string; postalCode?: string }): void {
    const cities = COVERAGE_AREA.find((p) => p.name === model.province)?.cities ?? [];
    if (!cities.some((c) => c.name === model.cityMunicipality)) {
        model.cityMunicipality = cities.length === 1 ? cities[0].name : '';
    }

    applyCoverageCity(model);
}

export function applyCoverageCity(model: { province?: string; cityMunicipality?: string; postalCode?: string }): void {
    const city = COVERAGE_AREA.find((p) => p.name === model.province)?.cities.find((c) => c.name === model.cityMunicipality);
    if (city && !model.postalCode?.trim()) {
        model.postalCode = city.postalCode;
    }
}

export function coverageBarangays(province: string, city: string, current?: string): string[] {
    const names = COVERAGE_AREA.find((p) => p.name === province)?.cities.find((c) => c.name === city)?.barangays ?? [];
    return names.length && current && !names.includes(current) ? [...names, current] : names;
}

export function applyCoverageBarangay(model: { province?: string; cityMunicipality?: string; addressLine2?: string }): void {
    const names = coverageBarangays(model.province, model.cityMunicipality);
    if (names.length && model.addressLine2 && !names.includes(model.addressLine2)) {
        model.addressLine2 = '';
    }
}
