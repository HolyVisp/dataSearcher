import { useMemo } from 'react';
import PropTypes from 'prop-types';
import { Button, Input, Select, Space, Tooltip } from 'antd';
import { PlusOutlined, UploadOutlined } from '@ant-design/icons';

const { Search } = Input;

export function SearchToolbar({
  query,
  onQueryChange,
  onSearch,
  datasets,
  selectedDataset,
  onDatasetChange,
  loading,
  onOpenManualDrawer,
  onOpenUploadModal
}) {
  const datasetOptions = useMemo(
    () =>
      (datasets || []).map((dataset) => ({
        label: dataset,
        value: dataset
      })),
    [datasets]
  );

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Search
        placeholder="Введите строку поиска (например, Петр)"
        allowClear
        enterButton="Поиск"
        size="large"
        value={query}
        loading={loading}
        onChange={(event) => onQueryChange(event.target.value)}
        onSearch={onSearch}
      />
      <Space className="table-toolbar">
        <Select
          allowClear
          showSearch
          placeholder="Все датасеты"
          style={{ minWidth: 220 }}
          options={datasetOptions}
          value={selectedDataset || null}
          onChange={(value) => onDatasetChange(value || undefined)}
          optionFilterProp="label"
        />
        <Tooltip title="Добавить запись вручную">
          <Button type="primary" icon={<PlusOutlined />} onClick={onOpenManualDrawer}>
            Новая запись
          </Button>
        </Tooltip>
        <Tooltip title="Загрузить CSV или JSON файл">
          <Button icon={<UploadOutlined />} onClick={onOpenUploadModal}>
            Загрузить файл
          </Button>
        </Tooltip>
      </Space>
    </Space>
  );
}

SearchToolbar.propTypes = {
  query: PropTypes.string.isRequired,
  onQueryChange: PropTypes.func.isRequired,
  onSearch: PropTypes.func.isRequired,
  datasets: PropTypes.arrayOf(PropTypes.string),
  selectedDataset: PropTypes.string,
  onDatasetChange: PropTypes.func.isRequired,
  loading: PropTypes.bool,
  onOpenManualDrawer: PropTypes.func.isRequired,
  onOpenUploadModal: PropTypes.func.isRequired
};

SearchToolbar.defaultProps = {
  datasets: [],
  selectedDataset: undefined,
  loading: false
};

export default SearchToolbar;
