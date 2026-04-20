import React from 'react';
import UploadTest from "./UploadTest";
import { Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import Home from './Pages/Home';
import AdminQueue from './Pages/AdminQueue';

const App = () => {
    return (
        <Layout>
            <Routes>
                <Route path='/' element={<Home />} />
                <Route path="/upload-test" element={<UploadTest />} />
                <Route path="/admin/queue" element={<AdminQueue />} />
            </Routes>
        </Layout>
    );
}

export default App;