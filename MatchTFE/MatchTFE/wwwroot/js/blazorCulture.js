window.blazorCulture = {
    get: () => window.localStorage['BlazorCulture'],
    set: (value) => { window.localStorage['BlazorCulture'] = value; }
};

window.blazorTheme = {
    get: () => window.localStorage.getItem('BlazorDarkMode'),
    set: (value) => { window.localStorage.setItem('BlazorDarkMode', value); }
};
