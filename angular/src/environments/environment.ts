export const environment = {
    production: false,
    appConfig: 'appconfig.json',
    devAccounts: [
        { group: 'Simple sample (FPMS_Simple)', label: 'Abi (owner)', farmCode: 'rkabubba-aqua', userName: 'arkabubba', password: 'Fpms@2026' },
        { group: 'Simple sample (FPMS_Simple)', label: 'Sam (owner)', farmCode: 'rkabubba-aqua', userName: 'samsedel', password: 'Fpms@2026' },
        { group: 'Simple sample (FPMS_Simple)', label: 'Kim (tech)', farmCode: 'rkabubba-aqua', userName: 'konacsus', password: 'Fpms@2026' },
        { group: 'Logbook sample (FPMS_Logbook)', label: 'Iba (owner)', farmCode: 'logbook-farm', userName: 'iabi', password: 'Fpms@2026' },
        { group: 'Logbook sample (FPMS_Logbook)', label: 'Mas (owner)', farmCode: 'logbook-farm', userName: 'msam', password: 'Fpms@2026' },
        { group: 'Logbook sample (FPMS_Logbook)', label: 'Mik (tech)', farmCode: 'logbook-farm', userName: 'mkim', password: 'Fpms@2026' },
        { group: 'System Administrator', label: 'Admin', farmCode: '', userName: 'admin', password: 'Fpms@2026' },
    ] as DevAccount[],
};

export interface DevAccount {
    group: string;
    label: string;
    farmCode: string;
    userName: string;
    password: string;
}
