import { useCallback, useEffect, useMemo, useState } from 'react';
import { Layout, message } from 'antd';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import dayjs from 'dayjs';
import SearchToolbar from './components/SearchToolbar.jsx';
import ResultsTable from './components/ResultsTable.jsx';
import ManualEntryDrawer from './components/ManualEntryDrawer.jsx';
import DatasetUploadModal from './components/DatasetUploadModal.jsx';
import { createManualRecord, fetchDatasets, searchRecords, uploadDatasetFile } from './services/api.js';

const { Content, Header } = Layout;

const initialPagination = {
  current: 1,
  pageSize: 25,
  showSizeChanger: true,
  pageSizeOptions: ['10', '25', '50', '100']
};

function App() {
  const [searchInput, setSearchInput] = useState('');
  const [query, setQuery] = useState('');
  const [dataset, setDataset] = useState();
  const [records, setRecords] = useState([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [datasets, setDatasets] = useState([]);
  const [pagination, setPagination] = useState(initialPagination);
  const [sort, setSort] = useState();
  const [manualDrawerOpen, setManualDrawerOpen] = useState(false);
  const [manualLoading, setManualLoading] = useState(false);
  const [uploadModalOpen, setUploadModalOpen] = useState(false);
  const [uploadLoading, setUploadLoading] = useState(false);

  const loadDatasets = useCallback(async () => {
    try {
      const response = await fetchDatasets();
      setDatasets(response);
    } catch (error) {
      console.error('Ошибка загрузки списка датасетов', error);
    }
  }, []);

  useEffect(() => {
    loadDatasets();
  }, [loadDatasets]);

  const executeSearch = useCallback(async () => {
    setLoading(true);
    try {
      const response = await searchRecords({
        query,
        page: pagination.current,
        pageSize: pagination.pageSize,
        dataset,
        sort
      });
      setRecords(response.results || []);
      setTotal(response.total || 0);
    } catch (error) {
      console.error('Ошибка поиска', error);
      message.error('Не удалось выполнить поиск');
    } finally {
      setLoading(false);
    }
  }, [query, pagination, dataset, sort]);

  useEffect(() => {
    executeSearch();
  }, [executeSearch]);

  const handleSearch = (value) => {
    setPagination((prev) => ({ ...prev, current: 1 }));
    setQuery(value);
    setSearchInput(value);
  };

  const handleDatasetChange = (value) => {
    setDataset(value);
    setPagination((prev) => ({ ...prev, current: 1 }));
  };

  const handleTableChange = (paginationConfig, _filters, sorterConfig) => {
    setPagination({
      current: paginationConfig.current,
      pageSize: paginationConfig.pageSize,
      showSizeChanger: true,
      pageSizeOptions: ['10', '25', '50', '100']
    });

    if (sorterConfig && sorterConfig.order && sorterConfig.field) {
      const order = sorterConfig.order === 'descend' ? 'desc' : 'asc';
      const field = sorterConfig.field;
      setSort(`${field}:${order}`);
    } else {
      setSort(undefined);
    }
  };

  const exportToPdf = () => {
    if (!records.length) {
      return;
    }

    const doc = new jsPDF({ orientation: 'landscape' });
    const columns = ['Датасет', 'Индекс', 'Релевантность', 'Создано', 'Данные'];
    const rows = records.map((record) => {
      const fieldData = Object.entries(record.fields)
        .map(([key, value]) => {
          if (Array.isArray(value)) {
            return `${key}: ${value.join(', ')}`;
          }
          if (value && typeof value === 'object') {
            return `${key}: ${JSON.stringify(value)}`;
          }
          return `${key}: ${value ?? ''}`;
        })
        .join('\n');

      return [
        record.dataset,
        record.index,
        record.score ? record.score.toFixed(3) : '',
        record.createdAt ? dayjs(record.createdAt).format('DD.MM.YYYY HH:mm') : '',
        fieldData
      ];
    });

    autoTable(doc, {
      head: [columns],
      body: rows,
      styles: { fontSize: 8, cellPadding: 2 },
      headStyles: { fillColor: [51, 102, 204] }
    });

    doc.save('search-results.pdf');
  };

  const handleManualSubmit = async ({ dataset: datasetName, fields }) => {
    setManualLoading(true);
    try {
      await createManualRecord({ dataset: datasetName, fields });
      message.success('Запись сохранена');
      setManualDrawerOpen(false);
      setPagination((prev) => ({ ...prev, current: 1 }));
      if (!datasets.includes(datasetName)) {
        await loadDatasets();
      }
      await executeSearch();
    } catch (error) {
      console.error('Ошибка ручного добавления', error);
      message.error('Не удалось добавить запись');
    } finally {
      setManualLoading(false);
    }
  };

  const handleUploadSubmit = async ({ dataset: datasetName, file }) => {
    setUploadLoading(true);
    try {
      await uploadDatasetFile({ dataset: datasetName, file });
      message.success('Файл принят, индексация выполняется');
      setUploadModalOpen(false);
      if (!datasets.includes(datasetName)) {
        await loadDatasets();
      }
      await executeSearch();
    } catch (error) {
      console.error('Ошибка загрузки файла', error);
      message.error('Не удалось загрузить файл');
    } finally {
      setUploadLoading(false);
    }
  };

  const tablePagination = useMemo(
    () => ({
      ...pagination,
      total
    }),
    [pagination, total]
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ background: '#1e293b' }}>
        <h1 style={{ color: '#fff', margin: 0 }}>Data Searcher</h1>
      </Header>
      <Content className="app-container">
        <SearchToolbar
          query={searchInput}
          onQueryChange={setSearchInput}
          onSearch={handleSearch}
          datasets={datasets}
          selectedDataset={dataset}
          onDatasetChange={handleDatasetChange}
          loading={loading}
          onOpenManualDrawer={() => setManualDrawerOpen(true)}
          onOpenUploadModal={() => setUploadModalOpen(true)}
        />

        <ResultsTable
          dataSource={records}
          loading={loading}
          total={total}
          pagination={tablePagination}
          onChange={handleTableChange}
          onExport={exportToPdf}
        />
      </Content>

      <ManualEntryDrawer
        open={manualDrawerOpen}
        onClose={() => setManualDrawerOpen(false)}
        onSubmit={handleManualSubmit}
        loading={manualLoading}
        defaultDataset={dataset || ''}
      />

      <DatasetUploadModal
        open={uploadModalOpen}
        onClose={() => setUploadModalOpen(false)}
        onSubmit={handleUploadSubmit}
        loading={uploadLoading}
        defaultDataset={dataset || ''}
      />
    </Layout>
  );
}

export default App;
